using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Horus.Modules.Core.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddJob(Job.Default
                .WithRuntime(CoreRuntime.Core80)
                .WithIterationCount(1) // Increased for more accurate results
                .WithWarmupCount(3) // Added extra warmup for embedding services
                .WithInvocationCount(3) // Multiple invocations per iteration
                .WithUnrollFactor(1)) // Important for async operations
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddExporter(RPlotExporter.Default)
            .AddExporter(HtmlExporter.Default)
            .WithArtifactsPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchmarkResults"));

        BenchmarkRunner.Run<GenerateTextBenchmarks>(config);
    }
}