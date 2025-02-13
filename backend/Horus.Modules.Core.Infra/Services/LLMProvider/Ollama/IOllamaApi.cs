using Refit;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;

public interface IOllamaApi
{
    [Post("/api/generate")]
    Task<GenerateResponse> GenerateAsync([Body] GenerateRequest request);

    [Post("/api/chat")]
    Task<GenerateResponse> ChatAsync([Body] GenerateRequest request);

    [Post("/api/embeddings")]
    Task<EmbeddingResponse> EmbeddingsAsync([Body] EmbeddingRequest request);
}