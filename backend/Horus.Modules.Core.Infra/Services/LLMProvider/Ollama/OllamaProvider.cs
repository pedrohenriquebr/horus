using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.Infra.Services.RateLimiter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;

public class OllamaProvider : ILlmProvider
{
    private readonly ILogger<OllamaProvider> _logger;
    private readonly IOllamaApi _ollamaApi;
    private readonly IOptions<OllamaConfig> _options;
    private readonly IRateLimiter _rateLimiter;

    public OllamaProvider(
        IOptions<OllamaConfig> options,
        IOllamaApi ollamaApi,
        ILogger<OllamaProvider> logger,
        IRateLimiter rateLimiter)
    {
        _options = options;
        _ollamaApi = ollamaApi;
        _logger = logger;
        _rateLimiter = rateLimiter;
    }

    public async Task<string> GenerateTextAsync(
        string prompt,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null)
    {
        try
        {
            if (!_rateLimiter.TryAcquire())
            {
                _logger.LogWarning("Rate limit exceeded, waiting...");
                await _rateLimiter.WaitAsync();
            }

            var messages = new List<Message>();

            if (chatHistory != null)
                messages.AddRange(chatHistory
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new Message
                    {
                        Role = m.Role,
                        Content = m.Content
                    }));

            messages.Add(new Message
            {
                Role = "user",
                Content = prompt
            });

            var request = new GenerateRequest
            {
                Model = _options.Value.ModelName,
                Messages = messages,
                System = systemInstruction?.GetValueOrDefault("text")?.ToString()
            };

            var response = await _ollamaApi.ChatAsync(request);
            return ExtractResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with Ollama");
            throw;
        }
    }


    public async Task<string> GenerateWithImageAsync(
        string imagePath,
        string? prompt = null,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null)
    {
        if (!_rateLimiter.TryAcquire())
        {
            _logger.LogWarning("Rate limit exceeded, waiting...");
            await _rateLimiter.WaitAsync();
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var base64Image = Convert.ToBase64String(imageBytes);

        var messages = new List<Message>();

        if (chatHistory != null)
            messages.AddRange(chatHistory
                .OrderBy(m => m.Timestamp)
                .Select(m => new Message
                {
                    Role = m.Role,
                    Content = m.Content
                }));

        messages.Add(new Message
        {
            Role = "user",
            Content = prompt ?? "Analyze this image",
            Images = new List<string> { base64Image }
        });

        var request = new GenerateRequest
        {
            Model = _options.Value.ModelName,
            Messages = messages,
            System = systemInstruction?.GetValueOrDefault("text")?.ToString()
        };

        var response = await _ollamaApi.ChatAsync(request);
        return ExtractResponse(response);
    }

    public async Task<string> GenerateWithAudioAsync(
        string audioPath,
        string? prompt = null,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null)
    {
        _logger.LogWarning("Audio processing not yet supported by Ollama");
        throw new NotImplementedException("Audio processing is not currently supported by Ollama");
    }

    private string ExtractResponse(GenerateResponse response)
    {
        if (string.IsNullOrEmpty(response.Response) && string.IsNullOrEmpty(response.Message?.Content))
            return "";

        if (string.IsNullOrEmpty(response.Response))
            return response.Message?.Content ?? string.Empty;

        return response.Response;
    }
}