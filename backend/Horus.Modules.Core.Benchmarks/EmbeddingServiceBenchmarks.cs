using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Infra.Services.Embedding;
using Microsoft.Extensions.DependencyInjection;

namespace Horus.Modules.Core.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class EmbeddingServiceBenchmarks
{
    private const string ShortText = "Quick test phrase";
    private readonly TestFixture _fixture;
    private readonly OllamaEmbeddingService _ollamaEmbeddingService;
    private readonly string LongText = new('x', 2000);
    private readonly string MediumText = new('x', 500);

    public EmbeddingServiceBenchmarks()
    {
        _fixture = new TestFixture();
        _ollamaEmbeddingService = _fixture.ServiceProvider.GetRequiredService<OllamaEmbeddingService>();
    }
    
    [Benchmark(Description = "Ollama - Short Text")]
    public async Task OllamaShortTextEmbedding()
    {
        await _ollamaEmbeddingService.GetEmbeddingAsync(ShortText);
    }

    
    [Benchmark(Description = "Ollama - Medium Text")]
    public async Task OllamaMediumTextEmbedding()
    {
        await _ollamaEmbeddingService.GetEmbeddingAsync(MediumText);
    }
    
    [Benchmark(Description = "Ollama - Long Text")]
    public async Task OllamaLongTextEmbedding()
    {
        await _ollamaEmbeddingService.GetEmbeddingAsync(LongText);
    }
}