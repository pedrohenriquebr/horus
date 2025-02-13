using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Horus.Modules.Core.Infra.Services.LLMProvider;

public class DefaultLlmProviderFallback : ILlmProvider
{
    private readonly GeminiProvider _primaryService;
    private readonly OllamaProvider _secondaryService;
    private readonly IServiceProvider _serviceProvider;

    public DefaultLlmProviderFallback(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _primaryService = _serviceProvider.GetRequiredService<GeminiProvider>();
        _secondaryService = _serviceProvider.GetRequiredService<OllamaProvider>();
    }

    public async Task<string> GenerateTextAsync(string prompt, Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null)
    {
        var fallbackPolicy = Policy<string>.Handle<HttpRequestException>().FallbackAsync(
            async cancellationToken =>
                await _secondaryService.GenerateTextAsync(prompt, systemInstruction, chatHistory));

        return await fallbackPolicy.ExecuteAsync(() =>
            _primaryService.GenerateTextAsync(prompt, systemInstruction, chatHistory));
    }

    public async Task<string> GenerateWithImageAsync(string imagePath, string? prompt = null,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null)
    {
        var fallbackPolicy = Policy<string>.Handle<HttpRequestException>().FallbackAsync(
            async cancellationToken =>
                await _secondaryService.GenerateWithImageAsync(imagePath, prompt, systemInstruction, chatHistory));

        return await fallbackPolicy.ExecuteAsync(() =>
            _primaryService.GenerateWithImageAsync(imagePath, prompt, systemInstruction, chatHistory));
    }

    public async Task<string> GenerateWithAudioAsync(string audioPath, string? prompt = null,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null)
    {
        var fallbackPolicy = Policy<string>.Handle<HttpRequestException>().FallbackAsync(
            async cancellationToken =>
                await _secondaryService.GenerateWithImageAsync(audioPath, prompt, systemInstruction, chatHistory));

        return await fallbackPolicy.ExecuteAsync(() =>
            _primaryService.GenerateWithImageAsync(audioPath, prompt, systemInstruction, chatHistory));
    }
}