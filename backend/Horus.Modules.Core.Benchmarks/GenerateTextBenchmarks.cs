using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Application.Usecases.GenerateText;
using Horus.Modules.Core.Domain.LLM;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Horus.Modules.Core.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RPlotExporter]
[HtmlExporter]
public class GenerateTextBenchmarks
{
    private readonly IChatHistoryProvider _chatHistoryProvider;
    private readonly TestFixture _fixture;
    private readonly IMediator _mediator;
    private readonly IMemoryProvider _memoryProvider;
    private readonly Dictionary<string, string> _userInfo;

    public GenerateTextBenchmarks()
    {
        _fixture = new TestFixture();
        _mediator = _fixture.ServiceProvider.GetRequiredService<IMediator>();
        _chatHistoryProvider = _fixture.ServiceProvider.GetRequiredService<IChatHistoryProvider>();
        _memoryProvider = _fixture.ServiceProvider.GetRequiredService<IMemoryProvider>();
        _userInfo = new Dictionary<string, string>
        {
            ["id"] = "benchmark_user",
            ["first_name"] = "Benchmark",
            ["username"] = "benchmark_test",
            ["language_code"] = "en"
        };
    }

    [Benchmark(Baseline = true)]
    public async Task SimplePrompt()
    {
        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "What is 2+2?",
            UserInfo = _userInfo
        });
    }

    [Benchmark]
    public async Task LongPrompt()
    {
        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = new string('x', 1000),
            UserInfo = _userInfo
        });
    }

    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    public async Task WithChatHistory(int historySize)
    {
        var history = new List<ChatMessage>();
        for (var i = 0; i < historySize; i++)
            history.Add(new ChatMessage
            {
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = $"Message {i}",
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                UserInfo = _userInfo
            });

        await _chatHistoryProvider.StoreMessagesAsync(_userInfo, history);

        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Continue the conversation",
            UserInfo = _userInfo
        });
    }

    [Benchmark]
    public async Task WithSystemInstruction()
    {
        var systemInstruction = "You are a helpful assistant that speaks in a professional tone.";

        await _mediator.Send(new GenerateTextQuery
        {
            Prompt = "Explain quantum computing",
            UserInfo = _userInfo,
            SystemInstructions = systemInstruction
        });
    }

    
    public async Task Cleanup()
    {
        await _chatHistoryProvider.ClearHistoryAsync(_userInfo);
        await _memoryProvider.ClearMemoriesAsync(_userInfo);
    }
}