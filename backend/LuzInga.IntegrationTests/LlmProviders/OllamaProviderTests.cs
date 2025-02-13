using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;
using Horus.Modules.Core.IntegrationTests;
using Microsoft.Extensions.DependencyInjection;

namespace LuzInga.IntegrationTests.LlmProviders;

public class OllamaProviderTests : IClassFixture<TestFixture>
{
    private readonly ILlmProvider _ollamaProvider;
    private readonly IServiceProvider _serviceProvider;

    public OllamaProviderTests(TestFixture fixture)
    {
        _serviceProvider = fixture.ServiceProvider;
        _ollamaProvider = _serviceProvider.GetRequiredService<OllamaProvider>();
        Thread.Sleep(5000); // Rate limit cooldown
    }

    [Fact]
    public async Task GenerateTextAsync_WithValidPrompt_ReturnsResponse()
    {
        // Arrange
        var prompt =
            "What is the capital of France? Answer with the name of the capital once with no additional comments";

        // Act
        var result = await _ollamaProvider.GenerateTextAsync(prompt);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Paris", Exactly.Once());
    }

    [Fact]
    public async Task GenerateTextAsync_WithSystemInstruction_ModifiesResponse()
    {
        // Arrange
        var prompt = "Tell me a story";
        var systemInstruction = new Dictionary<string, object>
        {
            ["text"] = "You are a poet who speaks in rhymes"
        };

        // Act
        var result = await _ollamaProvider.GenerateTextAsync(prompt, systemInstruction);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\n"); // Should contain line breaks for poetry
    }

    [Fact]
    public async Task GenerateTextAsync_WithChatHistory_MaintainsContext()
    {
        // Arrange
        var chatHistory = new List<ChatMessage>
        {
            new() { Role = "user", Content = "My name is John", Timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new()
            {
                Role = "assistant", Content = "Hello John, nice to meet you", Timestamp = DateTime.UtcNow.AddMinutes(-1)
            }
        };
        var prompt = "What's my name?";

        // Act
        var result = await _ollamaProvider.GenerateTextAsync(prompt, null, chatHistory);

        // Assert
        result.Should().Contain("John");
    }

    [Fact]
    public async Task GenerateWithImageAsync_WithValidImage_ReturnsDescription()
    {
        // Arrange
        var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LlmProviders", "TestData",
            "test-image.jpg");
        var prompt = "Describe this image in detail";

        // Act
        var result = await _ollamaProvider.GenerateWithImageAsync(imagePath, prompt);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeGreaterThan(100); // Detailed description
    }

    [Fact]
    public async Task GenerateWithImageAsync_WithChatHistory_MaintainsContext()
    {
        // Arrange
        var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LlmProviders", "TestData",
            "test-image.jpg");
        var chatHistory = new List<ChatMessage>
        {
            new() { Role = "user", Content = "I'll show you an image", Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new() { Role = "assistant", Content = "I'm ready to analyze it", Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = await _ollamaProvider.GenerateWithImageAsync(
            imagePath,
            "What do you see?",
            null,
            chatHistory);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateTextAsync_WithLongPrompt_HandlesCorrectly()
    {
        // Arrange
        var prompt = new string('x', 2000);

        // Act
        var result = await _ollamaProvider.GenerateTextAsync(prompt);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("english", "Hello")]
    [InlineData("portuguese", "Olá")]
    [InlineData("spanish", "Hola")]
    public async Task GenerateTextAsync_WithDifferentLanguages_RespondsAccordingly(string language, string expectedWord)
    {
        // Arrange
        var prompt = $"How to say 'hello' in {language}?";

        // Act
        var result = await _ollamaProvider.GenerateTextAsync(prompt);

        // Assert
        result.ToLower().Should().Contain(expectedWord.ToLower());
    }

    [Fact]
    public async Task GenerateWithImageAsync_InvalidImagePath_ThrowsFileNotFoundException()
    {
        // Arrange
        var invalidPath = "nonexistent.jpg";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _ollamaProvider.GenerateWithImageAsync(invalidPath));
    }

    [Fact]
    public async Task GenerateWithAudioAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var audioPath = "test.mp3";

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            _ollamaProvider.GenerateWithAudioAsync(audioPath));
    }
}