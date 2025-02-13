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

    public Task<float[]> GetEmbeddingWithFallbackAsync(string text)
    {
        throw new NotImplementedException();
    }
}