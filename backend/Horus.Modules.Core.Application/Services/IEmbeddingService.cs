namespace Horus.Modules.Core.Application.Services;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
    Task<float[]> GetEmbeddingWithFallbackAsync(string text);
}