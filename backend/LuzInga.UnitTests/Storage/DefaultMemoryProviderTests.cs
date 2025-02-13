using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Infra.Services.RAG;
using Microsoft.Extensions.Logging;
using Moq;

namespace LuzInga.UnitTests.Storage;

public class DefaultMemoryProviderTests : IDisposable
{
    private readonly ILogger<DefaultMemoryProvider> _logger;
    private readonly DefaultMemoryProvider _provider;
    private readonly IRagService _ragService;
    private readonly Dictionary<string, string> _testUserInfo;

    public DefaultMemoryProviderTests()
    {
        // Setup mock dependencies
        _ragService = Mock.Of<IRagService>();
        _logger = Mock.Of<ILogger<DefaultMemoryProvider>>();

        // Initialize the provider with mocked dependencies
        _provider = new DefaultMemoryProvider(_ragService, _logger);

        // Setup test user info
        _testUserInfo = new Dictionary<string, string>
        {
            ["id"] = "test-user-id"
        };
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task StoreMemoryAsync_ShouldReturnTrue_WhenValidInput()
    {
        // Arrange
        var memoryItem = new MemoryItem(
            "Test memory content",
            DateTime.UtcNow,
            "test-source",
            new Dictionary<string, object>()
        );

        Mock.Get(_ragService)
            .Setup(x => x.AddDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(new Document());

        // Act
        var result = await _provider.StoreMemoryAsync(memoryItem, _testUserInfo);

        // Assert
        Assert.True(result);
        Mock.Get(_ragService).Verify(
            x => x.AddDocumentAsync(
                It.Is<string>(content => content == memoryItem.Content),
                It.Is<Dictionary<string, object>>(metadata =>
                    metadata["type"].ToString() == "memory" &&
                    metadata["user_id"].ToString() == _testUserInfo["id"])),
            Times.Once);
    }

    [Fact]
    public async Task GetMemoriesAsync_ShouldReturnMemories_WhenValidUserInfo()
    {
        // Arrange
        var testDocs = new List<Document>
        {
            new()
            {
                Content = "Test memory 1",
                CreatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = "memory",
                    ["user_id"] = _testUserInfo["id"],
                    ["metadata"] = new Dictionary<string, object>
                    {
                        ["source"] = "test-source"
                    }
                }
            }
        };

        Mock.Get(_ragService)
            .Setup(x => x.GetAllMemoriesByUserId(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(testDocs);

        // Act
        var memories = await _provider.GetMemoriesAsync(_testUserInfo);

        // Assert
        Assert.NotEmpty(memories);
        var memory = memories.First();
        Assert.Equal("Test memory 1", memory.Content);
        Assert.Equal("test-source", memory.Source);
    }

    [Fact]
    public async Task UpdateWorkingMemoryAsync_ShouldAddDocument_WhenValidUserInfo()
    {
        // Arrange
        var testQuery = "test query";

        // Act
        await _provider.UpdateWorkingMemoryAsync(testQuery, _testUserInfo);

        // Assert
        Mock.Get(_ragService).Verify(
            x => x.AddDocumentAsync(
                It.Is<string>(content => content == testQuery),
                It.Is<Dictionary<string, object>>(metadata =>
                    metadata["type"].ToString() == "working_memory" &&
                    metadata["user_id"].ToString() == _testUserInfo["id"])),
            Times.Once);
    }

    [Fact]
    public async Task GetMemoriesAsync_ShouldReturnEmpty_WhenUserIdIsNull()
    {
        // Arrange
        var userInfoWithoutId = new Dictionary<string, string>();

        // Act
        var memories = await _provider.GetMemoriesAsync(userInfoWithoutId);

        // Assert
        Assert.Empty(memories);
        Mock.Get(_ragService).Verify(
            x => x.SearchSimilarAsync(It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task StoreMemoryAsync_ShouldReturnFalse_WhenUserIdIsNull()
    {
        // Arrange
        var memoryItem = new MemoryItem(
            "Test memory content",
            DateTime.UtcNow,
            "test-source",
            new Dictionary<string, object>()
        );
        var userInfoWithoutId = new Dictionary<string, string>();

        // Act
        var result = await _provider.StoreMemoryAsync(memoryItem, userInfoWithoutId);

        // Assert
        Assert.False(result);
        Mock.Get(_ragService).Verify(
            x => x.AddDocumentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()),
            Times.Never);
    }
}