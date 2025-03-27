using System.Text;
using System.Text.RegularExpressions;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.LLM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ChatMessage = Horus.Modules.Core.Domain.Entities.ChatMessage;

namespace Horus.Modules.Core.Application.Usecases.GenerateText;

public class GenerateTextQueryHandler : IQueryHandler<GenerateTextQuery, GenerateTextQueryResponse>
{
    private readonly IChatHistoryProvider _chatHistoryProvider;
    private readonly ILlmProvider _llmClient;
    private readonly ILogger<GenerateTextQueryHandler> _logger;
    private readonly IMemoryProcessor _memoryProcessor;
    private readonly IMemoryProvider _memoryProvider;
    private readonly ISystemInstructionBuilder _systemInstructionBuilder;
    private IRagService _ragService;
    private readonly IHorusContext _horusContext;

    public GenerateTextQueryHandler(
        ILlmProvider llmClient,
        ISystemInstructionBuilder systemInstructionBuilder,
        IMemoryProcessor memoryProcessor,
        IChatHistoryProvider chatHistoryProvider,
        IMemoryProvider memoryProvider,
        ILogger<GenerateTextQueryHandler> logger,
        IRagService ragService,
        IHorusContext horusContext)
    {
        _llmClient = llmClient;
        _systemInstructionBuilder = systemInstructionBuilder;
        _memoryProcessor = memoryProcessor;
        _chatHistoryProvider = chatHistoryProvider;
        _memoryProvider = memoryProvider;
        _logger = logger;
        _ragService = ragService;
        _horusContext = horusContext;
    }

    public async Task<GenerateTextQueryResponse> Handle(GenerateTextQuery request, CancellationToken cancellationToken)
    {
        if (request.UserInfo is null || request.UserInfo?.GetValueOrDefault("id") is null)
        {
            return await GenerateTextOnly(request);
        }

        return await GenerateTextForChatting(request);
    }

    private async Task<GenerateTextQueryResponse> GenerateTextForChatting(GenerateTextQuery request)
    {
        var chatHistory = await _chatHistoryProvider.GetHistoryAsync(request.UserInfo);
        var relevantMemories = (await _memoryProvider.GetMemoriesAsync(request.UserInfo)).ToList();
        var relevantDocs = (await _ragService.SearchSimilarHybridAsync(request.Prompt, Guid.Parse(request.UserInfo.GetValueOrDefault("id")!))).ToList();
        var systemInstruction = _systemInstructionBuilder.Build(request.UserInfo);
        

        List<SourceResponse>? sources = null;

        if (relevantDocs.Any() || relevantMemories.Any())
        {
            //memory
            //web
            //docs
            
            sources = new List<SourceResponse>();
            sources.AddRange(relevantMemories.Select(d =>
            {
                return new SourceResponse()
                {
                    Type = "memory",
                    Content = d.Content,
                    CreatedAt = d.CreatedAt,
                    Url = null,
                    DocumentName = null
                };
            }));


            foreach (var doc in relevantDocs)
            {
                var originalChunk = await this._horusContext.DocumentChunks.FindAsync(doc.ChunkId);
                if (originalChunk == null) continue;
                //TODO: get the original document name
                // var originalDoc = await this._horusContext.Documents.FindAsync(originalChunk?.DocumentId);
                sources.Add(new SourceResponse()
                {
                    Type = "chunk",
                    Content = doc.Content,
                    CreatedAt = originalChunk.CreatedAt,
                    Url = null,
                    DocumentName = "TODO"
                });
            }
               
        }
        
        var userPrompt = _BuildUserPrompt(relevantMemories, relevantDocs, request.Prompt, request.UserInfo);
        var response = await GenerateResponse(request, userPrompt, systemInstruction, chatHistory);

        var usedSources = Regex.Match(response, @"\[(\d+(?:,\s+\d+)*)\]").Success;
        var processedResponse = await _memoryProcessor.ProcessMemoryTags(response, request.UserInfo);


        await _chatHistoryProvider.StoreMessageAsync("user", request.Prompt, request.UserInfo);
        var addMetadata = sources != null && usedSources ? 
            new Dictionary<string, object>()
        {
            ["sources"] = sources
        }: null;
        await _chatHistoryProvider.StoreMessageAsync("assistant", processedResponse, request.UserInfo, addMetadata);

        
        //return only if the response contains [\d] reference
     

        return new GenerateTextQueryResponse
        {
            Text = processedResponse,
            Sources = usedSources ? sources : null,
        };
    }

    private async Task<GenerateTextQueryResponse> GenerateTextOnly(GenerateTextQuery request)
    {
        var responseWithNoUser = await GenerateResponse(request, userPrompt: request.Prompt, request.SystemInstructions,
            chatHistory: new List<Domain.LLM.ChatMessage>());

        return new GenerateTextQueryResponse
        {
            Text = responseWithNoUser,
        };
    }

    private async Task<string> GenerateResponse(GenerateTextQuery request, string userPrompt, string systemInstruction,
        IEnumerable<Domain.LLM.ChatMessage> chatHistory)
    {
        string response;
        Dictionary<string, object> systemInstuctionDict = string.IsNullOrEmpty(systemInstruction)
            ? new Dictionary<string, object>()
            : new Dictionary<string, object> { ["text"] = systemInstruction };
        
        
        if (!string.IsNullOrEmpty(request.ImagePath))
            response = await _llmClient.GenerateWithImageAsync(
                request.ImagePath!,
                userPrompt,
                systemInstuctionDict,
                chatHistory.ToList());
        else if (!string.IsNullOrEmpty(request.AudioPath))
            response = await _llmClient.GenerateWithAudioAsync(
                request.AudioPath!,
                userPrompt,
                systemInstuctionDict,
                chatHistory.ToList());
        else
            response = await _llmClient.GenerateTextAsync(
                userPrompt,
                systemInstuctionDict,
                chatHistory.ToList());
        return response;
    }

    private string _BuildUserPrompt(IEnumerable<MemoryItem> relevantMemories,
        IEnumerable<HybridSearchResult> relevantDocs,
        string requestPrompt,
        Dictionary<string, string> userInfo)
    {
        if (userInfo.GetValueOrDefault("id") is null)
            return requestPrompt;

        var builder = new StringBuilder();
        var builderForMemory = new StringBuilder();
        var builderForDocs = new StringBuilder();
        builder.Append("<informações_do_sistema>\n\n");
        builder.Append("Data atual: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        builder.Append("\nDia da semana: " + DateTime.Now.ToString("dddd"));
        builder.Append("\nFuso horário: " + TimeZoneInfo.Local.DisplayName);
        builder.Append("\nSistema Operacional: " + Environment.OSVersion);
        builder.Append("\nVersão do .NET: " + Environment.Version);
        builder.Append("\nNome da máquina: " + Environment.MachineName);
        builder.Append("\nMemória disponível: " + GC.GetTotalMemory(false) / 1024 / 1024 + " MB");
        builder.Append("\nProcessadores lógicos: " + Environment.ProcessorCount);
        builder.Append("\nDiretório atual: " + Environment.CurrentDirectory);
        builder.Append("\n\n</informações_do_sistema>\n");

        if (userInfo.ContainsKey("id"))
        {
            builder.Append("\n\n<informações_do_usuario_atual>\n\n");

            if (userInfo.ContainsKey("first_name")) builder.Append($"\nO nome do usuário é {userInfo["first_name"]}.");

            if (userInfo.ContainsKey("username")) builder.Append($"\nusername: {userInfo["username"]}.");

            if (userInfo.ContainsKey("language_code"))
                builder.Append($"\nIdioma preferido: {userInfo["language_code"]}.");

            builder.Append("\n</informações_do_usuario_atual>\n");
        }
        
        int number = 1;
        var items = relevantMemories.ToList();
        if (relevantMemories.Any())
        {
            
            
            foreach (var doc in items)
            {
                builderForMemory.Append("<memory>\n");
                builderForMemory.Append("<content>" + doc.Content + "</content>\n");
                builderForMemory.Append("<source_number>" + number + "</source_number>\n");
                builderForMemory.Append("<created_at>" + doc.CreatedAt.ToString("O") + "</created_at>\n");
                builderForMemory.Append("</memory>\n");
                number++;
            }
        }
        // if (items.IsNullOrEmpty())
        //
        //     return
        //         builder +
        //         "Quando o usuário compartilhar informações pessoais (como preferências, datas importantes ou detalhes biográficos), \n" +
        //         "envolva o fato relevante em tags <store_memory>fato</store_memory>\n" +
        //         "responda de forma natural o seguinte prompt do usuario e use a tag <store_memory> para armazenar informações sobre o usuário atual:\n"
        //         + requestPrompt;
        
        foreach (var doc in relevantDocs)
        {
            builderForDocs.Append("<document>\n");
            builderForDocs.Append("<content>" + doc.Content + "</content>\n");
            builderForDocs.Append("<source_number>" + number + "</source_number>\n");
            builderForDocs.Append("</document>\n");
            number++;
        }
        
        builder.Append("Memorias relevantes relacionadas ao usuario atual:\n\n");
        builder.Append("<memories>\n");
        builder.Append(builderForMemory);
        builder.Append("</memories>\n");


        builder.Append("<relevant_documents>\n");
        builder.Append(builderForDocs);
        builder.Append("</relevant_documents>\n");


        return
            builder +
            "INSTRUÇÕES CRÍTICAS PARA A RESPOSTA:\n" +
            "1. Use as fontes APENAS quando forem diretamente relevantes para a pergunta\n" +
            "2. Sempre priorize o contexto mais específico e relacionado ao prompt\n" +
            "3. Se nenhuma fonte for relevante, não faça nenhuma citação\n" +
            "4. Formato de citação: [número] correspondente à tag <source_number>\n\n" +
        
            "COMO REFERENCIAR:\n" +
            "- Cite explicitamente SOMENTE quando a fonte for essencial para a resposta\n" +
            "- Exemplo correto: 'De acordo com os registros [3], os dados mostram...'\n" +
            "- Exemplo a evitar: 'Existem algumas fontes [1][4]...' (se não forem usadas na resposta)\n\n" +
        
            "MEMORIAS E DOCUMENTOS SÃO CONTEXTOS AUXILIARES:\n" +
            // "- Não mencione a existência das memórias/documentos\n" +
            // "- Não liste fontes por educação ou protocolo\n" +
            "- Se a informação não constar nas fontes, responda normalmente sem citações\n\n" +
        
            "ARMAZENAMENTO DE INFORMAÇÕES:\n" +
            "Ao identificar novos dados pessoais do usuário (preferências, datas, detalhes biográficos, senhas):\n" +
            "- Você deve responder envolvendo em <store_memory>informação</store_memory>\n" +
            "- Mantenha a naturalidade da conversa\n\n" +
            "- Toda vez que o usuário compartilhar gostos pessoais (filmes, músicas, livros, etc.), envolva em <store_memory>gosto</store_memory>\n" +
        
            "RESPONDA DE FORMA NATURAL PARA:\n" +
            requestPrompt;
    
    }
}