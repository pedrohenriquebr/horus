using System.Text;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Horus.Modules.Core.Application.Usecases.GenerateText;

public class GenerateTextQueryHandler : IQueryHandler<GenerateTextQuery, GenerateTextQueryResponse>
{
    private readonly IChatHistoryProvider _chatHistoryProvider;
    private readonly ILlmProvider _llmClient;
    private readonly ILogger<GenerateTextQueryHandler> _logger;
    private readonly IMemoryProcessor _memoryProcessor;
    private readonly IMemoryProvider _memoryProvider;
    private readonly ISystemInstructionBuilder _systemInstructionBuilder;

    public GenerateTextQueryHandler(
        ILlmProvider llmClient,
        ISystemInstructionBuilder systemInstructionBuilder,
        IMemoryProcessor memoryProcessor,
        IChatHistoryProvider chatHistoryProvider,
        IMemoryProvider memoryProvider,
        ILogger<GenerateTextQueryHandler> logger)
    {
        _llmClient = llmClient;
        _systemInstructionBuilder = systemInstructionBuilder;
        _memoryProcessor = memoryProcessor;
        _chatHistoryProvider = chatHistoryProvider;
        _memoryProvider = memoryProvider;
        _logger = logger;
    }

    public async Task<GenerateTextQueryResponse> Handle(GenerateTextQuery request, CancellationToken cancellationToken)
    {
        var chatHistory = await _chatHistoryProvider.GetHistoryAsync(request.UserInfo);
        var systemInstruction = _systemInstructionBuilder.Build(request.UserInfo);
        var relevantDocs = await _memoryProvider.GetMemoriesAsync(request.UserInfo);
        var userPrompt = _BuildUserPrompt(relevantDocs, request.Prompt, request.UserInfo);

        string response;

        if (!string.IsNullOrEmpty(request.ImagePath))
            response = await _llmClient.GenerateWithImageAsync(
                request.ImagePath!,
                userPrompt,
                new Dictionary<string, object> { ["text"] = systemInstruction },
                chatHistory.ToList());
        else if (!string.IsNullOrEmpty(request.AudioPath))
            response = await _llmClient.GenerateWithAudioAsync(
                request.AudioPath!,
                userPrompt,
                new Dictionary<string, object> { ["text"] = systemInstruction },
                chatHistory.ToList());
        else
            response = await _llmClient.GenerateTextAsync(
                userPrompt,
                new Dictionary<string, object> { ["text"] = systemInstruction },
                chatHistory.ToList());

        var processedResponse = await _memoryProcessor.ProcessMemoryTags(response, request.UserInfo);

        await _chatHistoryProvider.StoreMessageAsync("user", request.Prompt, request.UserInfo);
        await _chatHistoryProvider.StoreMessageAsync("model", processedResponse, request.UserInfo);

        return new GenerateTextQueryResponse { Text = processedResponse };
    }

    private string _BuildUserPrompt(IEnumerable<MemoryItem> relevantDocs, string requestPrompt,
        Dictionary<string, string> userInfo)
    {
        var builder = new StringBuilder();
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

        builder.Append("\n\n<informações_do_usuario_atual>\n\n");

        if (userInfo.ContainsKey("first_name")) builder.Append($"\nO nome do usuário é {userInfo["first_name"]}.");

        if (userInfo.ContainsKey("username")) builder.Append($"\nusername: {userInfo["username"]}.");

        if (userInfo.ContainsKey("language_code")) builder.Append($"\nIdioma preferido: {userInfo["language_code"]}.");

        builder.Append("\n</informações_do_usuario_atual>\n");

        var items = relevantDocs.ToList();
        if (items.IsNullOrEmpty())

            return
                builder +
                "Quando o usuário compartilhar informações pessoais (como preferências, datas importantes ou detalhes biográficos), \n" +
                "envolva o fato relevante em tags <store_memory>fato</store_memory>\n" +
                "responda de forma natural o seguinte prompt do usuario e use a tag <store_memory> para armazenar informações sobre o usuário atual:\n"
                + requestPrompt;

        var builderForMemory = new StringBuilder();
        foreach (var doc in items)
        {
            builderForMemory.Append("<memory>\n");
            builderForMemory.Append("<content>" + doc.Content + "</content>\n");
            builderForMemory.Append("<created_at>" + doc.CreatedAt.ToString("O") + "</created_at>\n");
            builderForMemory.Append("</memory>\n");
        }


        return
            builder +
            "Memorias relevantes relacionadas ao usuario atual:\n\n"
            + "<memories>\n"
            + builderForMemory
            + "</memories>\n"
            + "VOCÊ DEVE Para formatação USAR APENAS:\n" +
            "- <i>texto</i> para itálico\n" +
            "- <b>texto</b> para negrito\n" +
            "- Use - ou números para listas\n" +
            "- NÃO use markdown (* ou _)\n"
            + "Com base nesse contexto dado, \n" +
            "Quando o usuário compartilhar informações pessoais (como preferências, datas importantes ou detalhes biográficos), \n" +
            "envolva o fato relevante em tags <store_memory>fato</store_memory>\n" +
            "responda de forma natural o seguinte prompt do usuario e use a tag <store_memory> para armazenar informações sobre o usuário atual:\n"
            + requestPrompt;
    }
}