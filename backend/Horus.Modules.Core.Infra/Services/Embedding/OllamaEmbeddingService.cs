using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Horus.Modules.Core.Infra.Services.Embedding;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly ILogger<OllamaProvider> _logger;
    private readonly IOllamaApi _ollamaApi;
    private readonly IOptions<OllamaConfig> _options;

    public OllamaEmbeddingService(IOptions<OllamaConfig> options, IOllamaApi ollamaApi, ILogger<OllamaProvider> logger)
    {
        _options = options;
        _ollamaApi = ollamaApi;
        _logger = logger;
    }


    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var result = await _ollamaApi.EmbeddingsAsync(new EmbeddingRequest
        {
            Model = _options.Value.EmbeddingModel,
            Prompt = text
        });

        return result.Embedding;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, string embeddingModel)
    {
        var result = await _ollamaApi.EmbeddingsAsync(new EmbeddingRequest
        {
            Model = embeddingModel,
            Prompt = text
        });

        return result.Embedding;
    }

    // Método para calcular tokens
    public async Task<int> CalculateToken(string text)
    {
        string model = this._options.Value.ModelName;
        try
        {
            // Enviando o texto para a API para obter a resposta de geração

            var response = await _ollamaApi.GenerateAsync(new GenerateRequest
            {
                Model = model,
                Prompt = text,
                Stream = false // Obtém a resposta completa de uma vez
            });

            // O número de tokens no prompt
            int promptTokens = response.PromptEvalCount ?? 0;

            // O número de tokens na resposta
            int responseTokens = response.EvalCount ?? 0;

            // Logando as informações
            _logger.LogInformation(
                $"Modelo: {model}, Tokens no Prompt: {promptTokens}, Tokens na Resposta: {responseTokens}");

            // Retornando o total de tokens
            return promptTokens + responseTokens;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao calcular tokens para o modelo {model}: {ex.Message}");
            throw;
        }
    }

    public Task<float[]> GetEmbeddingWithFallbackAsync(string text)
    {
        throw new NotImplementedException();
    }
}