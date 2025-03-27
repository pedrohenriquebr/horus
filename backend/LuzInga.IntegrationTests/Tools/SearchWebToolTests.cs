using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.Infra.Services.LLMProvider;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;
using Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;
using Horus.Modules.Core.IntegrationTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LuzInga.IntegrationTests.Tools;

public class SearchWebToolTests : IClassFixture<TestFixture>, IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SearchWebTool _searchWebTool;
    private readonly OllamaProvider _ollamaProvider;
    private readonly GeminiProvider _geminiProvider;
    private Dictionary<string, string>? _userInfo;

    public SearchWebToolTests(TestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
        var logger = _serviceProvider.GetRequiredService<ILogger<SearchWebTool>>();
        _searchWebTool = _serviceProvider.GetRequiredService<SearchWebTool>();
    }

    public async Task InitializeAsync()
    {
        _userInfo = new Dictionary<string, string>
        {
            { "userId", "test-user" },
            { "sessionId", Guid.NewGuid().ToString() }
        };
        
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        var chatHistory = _serviceProvider.GetRequiredService<IChatHistoryProvider>();
        var memoryProvider = _serviceProvider.GetRequiredService<IMemoryProvider>();
        
        if (_userInfo != null)
        {
            await chatHistory.ClearHistoryAsync(_userInfo);
            await memoryProvider.ClearMemoriesAsync(_userInfo);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithValidQuery_ReturnsSearchResults()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "Query", "What is C# programming language" },
            { "NumResults", 3 }
        };

        // Act
        var result = await _searchWebTool.ExecuteAsync(parameters);
        var searchResponse = (SearchResponse)result;

        // Assert
        Assert.NotNull(searchResponse);
        Assert.NotNull(searchResponse.Results);
        Assert.True(searchResponse.Results.Count > 0);
        Assert.Null(searchResponse.Error);
        Assert.NotNull(searchResponse.FinalSummary);

        // Verify result structure
        foreach (var searchResult in searchResponse.Results)
        {
            Assert.NotNull(searchResult.Title);
            Assert.NotNull(searchResult.Link);
            Assert.NotNull(searchResult.Snippet);
            Assert.NotNull(searchResult.ExtractedContent);
            Assert.NotNull(searchResult.Summary);
            Assert.True(Uri.IsWellFormedUriString(searchResult.Link, UriKind.Absolute));
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidQuery_ReturnsEmptyResults()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "Query", string.Empty }
        };

        // Act
        var result = await _searchWebTool.ExecuteAsync(parameters);
        var searchResponse = (SearchResponse)result;

        // Assert
        Assert.NotNull(searchResponse);
        Assert.NotNull(searchResponse.Results);
        Assert.Empty(searchResponse.Results);
        Assert.NotNull(searchResponse.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexQuery_ReturnsSummarizedResults()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "Query", "Explain the differences between .NET Framework, .NET Core, and .NET 5+" },
            { "NumResults", 2 }
        };

        // Act
        var result = await _searchWebTool.ExecuteAsync(parameters);
        var searchResponse = (SearchResponse)result;

        // Assert
        Assert.NotNull(searchResponse);
        Assert.NotNull(searchResponse.Results);
        Assert.True(searchResponse.Results.Count > 0);
        Assert.Null(searchResponse.Error);
        Assert.NotNull(searchResponse.FinalSummary);

        // Verify content extraction and summarization
        foreach (var searchResult in searchResponse.Results)
        {
            Assert.NotEmpty(searchResult.ExtractedContent);
            Assert.NotEmpty(searchResult.Summary);
            Assert.True(searchResult.Summary.Length < searchResult.ExtractedContent.Length);
        }

        // Verify final summary combines information from multiple sources
        Assert.Contains(".NET", searchResponse.FinalSummary, StringComparison.OrdinalIgnoreCase);
        Assert.True(searchResponse.FinalSummary.Length > 100);
    }

    [Fact]
    public async Task ExecuteAsync_CachingBehavior_ReusesCachedResults()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "Query", "What is Docker containerization" },
            { "NumResults", 1 }
        };

        // Act - First call
        var result1 = await _searchWebTool.ExecuteAsync(parameters);
        var response1 = (SearchResponse)result1;

        // Act - Second call with same query
        var result2 = await _searchWebTool.ExecuteAsync(parameters);
        var response2 = (SearchResponse)result2;

        // Assert
        Assert.NotNull(response1);
        Assert.NotNull(response2);
        Assert.Equal(
            JsonSerializer.Serialize(response1.Results.First()),
            JsonSerializer.Serialize(response2.Results.First())
        );
    }
}
