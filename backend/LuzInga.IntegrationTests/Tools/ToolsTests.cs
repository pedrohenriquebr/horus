using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.IntegrationTests;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuzInga.IntegrationTests;

public class ToolsTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly IConfiguration _configuration;
    private readonly IMediator _mediator;

    private readonly IServiceProvider _serviceProvider;
    private Dictionary<string, string>? _userInfo;

    public ToolsTests(TestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
        _configuration = fixture.Configuration;
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    public async Task InitializeAsync()
    {
        await Task.Delay(5000);
    }

    public async Task DisposeAsync()
    {
        var chatHistory = _serviceProvider.GetRequiredService<IChatHistoryProvider>();
        var memoryProvider = _serviceProvider.GetRequiredService<IMemoryProvider>();
        var userInfo = _userInfo;
        if (userInfo != null) await chatHistory.ClearHistoryAsync(userInfo);
        if (_userInfo != null) await memoryProvider.ClearMemoriesAsync(_userInfo);
    }
}