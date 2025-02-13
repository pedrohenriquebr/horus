using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;
using Horus.Modules.Core.Infra.Services.RateLimiter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Horus.Modules.Core.Infra.Services.LLMProvider;

public class GeminiProvider : ILlmProvider
{
    private readonly IOptions<GeminiConfig> _configuration;
    private readonly IGeminiApi _geminiApi;
    private readonly ILogger<GeminiProvider> _logger;
    private readonly IRateLimiter _rateLimiter;
    private readonly string _systemInstruction = null;
    private readonly IToolMediator _toolMediator;

    public GeminiProvider(
        IRateLimiter rateLimiter,
        ILogger<GeminiProvider> logger,
        IOptions<GeminiConfig> configuration,
        IGeminiApi geminiApi, IToolMediator toolMediator)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _geminiApi = geminiApi;
        _toolMediator = toolMediator;
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

            var systemInstructionText = _systemInstruction;
            if (systemInstruction != null &&
                systemInstruction.TryGetValue("text", out var instruction))
                systemInstructionText = instruction.ToString();

            var request = new GenerateContentRequest(
                    chatHistory
                        .OrderBy(d => d.Timestamp)
                        .Select(message => new Content(
                            message.Role,
                            new List<Part>
                            {
                                new(message.Content)
                            }
                        ))
                        .Append(new Content("user", new List<Part>
                        {
                            new(prompt)
                        }))
                        .ToList(),
                    SystemInstruction: new SystemInstruction(new List<Part>
                    {
                        new(systemInstructionText)
                    }),
                    tool_config: new ToolConfig(
                        new FunctionCallingConfig("AUTO"))
                )
                .AddWebSearchTool();

            var response = await _geminiApi.GenerateContentAsync(_configuration.Value.ModelName,
                _configuration.Value.ApiKey,
                request
            );


            if (response.HasFunctionCall())
            {
                var result = await response.HandleFunctionCallsAsync(_toolMediator);
                return result;
            }

            return ExtractResponseText(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with Gemini");
            throw;
        }
    }

    public async Task<string> GenerateWithImageAsync(string imagePath,
        string? prompt = null,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null) // Adicionado parâmetro de histórico
    {
        try
        {
            if (!_rateLimiter.TryAcquire())
            {
                _logger.LogWarning("Rate limit exceeded, waiting...");
                await _rateLimiter.WaitAsync();
            }

            var systemInstructionText = _systemInstruction;
            if (systemInstruction != null &&
                systemInstruction.TryGetValue("text", out var instruction))
                systemInstructionText = instruction.ToString();

            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var base64Image = Convert.ToBase64String(imageBytes);

            var contents = new List<Content>();

            // Adiciona histórico existente
            if (chatHistory != null)
                contents.AddRange(chatHistory
                    .OrderBy(d => d.Timestamp)
                    .Select(message => new Content(
                        message.Role,
                        new List<Part>
                        {
                            new(message.Content)
                        })));

            // Adiciona nova mensagem com imagem
            var imageParts = new List<Part>
            {
                new(InlineData: new InlineData(
                    GetMimeType(imagePath),
                    base64Image))
            };

            if (!string.IsNullOrEmpty(prompt)) imageParts.Insert(0, new Part(prompt));

            contents.Add(new Content("user", imageParts));

            var request = new GenerateContentRequest(
                    contents,
                    SystemInstruction: new SystemInstruction(new List<Part>
                    {
                        new(systemInstructionText)
                    })
                )
                .AddWebSearchTool();

            var response = await _geminiApi.GenerateContentAsync(
                _configuration.Value.ModelName,
                _configuration.Value.ApiKey,
                request);
            if (response.HasFunctionCall())
            {
                var result = await response.HandleFunctionCallsAsync(_toolMediator);
                return result;
            }

            return ExtractResponseText(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response with image");
            throw;
        }
    }

    public async Task<string> GenerateWithAudioAsync(
        string audioPath,
        string? prompt = null,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null) // Adicionado parâmetro de histórico
    {
        try
        {
            var systemInstructionText = _systemInstruction;
            if (systemInstruction != null &&
                systemInstruction.TryGetValue("text", out var instruction))
                systemInstructionText = instruction.ToString();

            var audioBytes = await File.ReadAllBytesAsync(audioPath);
            var base64Audio = Convert.ToBase64String(audioBytes);

            var contents = new List<Content>();

            // Adiciona histórico existente
            if (chatHistory != null)
                contents.AddRange(chatHistory
                    .OrderBy(d => d.Timestamp)
                    .Select(message => new Content(
                        message.Role,
                        new List<Part>
                        {
                            new(message.Content)
                        })));

            // Adiciona nova mensagem com áudio
            var audioParts = new List<Part>
            {
                new(InlineData: new InlineData(
                    GetMimeType(audioPath),
                    base64Audio))
            };

            if (!string.IsNullOrEmpty(prompt)) audioParts.Add(new Part(prompt));

            contents.Add(new Content("user", audioParts));

            var request = new GenerateContentRequest(
                    contents,
                    SystemInstruction: new SystemInstruction(new List<Part>
                    {
                        new(systemInstructionText)
                    })
                )
                .AddWebSearchTool();

            var response = await _geminiApi.GenerateContentAsync(
                _configuration.Value.ModelName,
                _configuration.Value.ApiKey,
                request);

            if (response.HasFunctionCall())
            {
                var result = await response.HandleFunctionCallsAsync(_toolMediator);
                return result;
            }

            return ExtractResponseText(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio");
            throw;
        }
    }

    private string ExtractResponseText(GenerateContentResponse response)
    {
        return response.Candidates[0].Content.Parts[0].Text;
    }

    private SystemInstruction? BuildSystemInstruction(Dictionary<string, object>? systemInstruction)
    {
        if (systemInstruction == null || !systemInstruction.TryGetValue("text", out var instruction))
            return null;

        return new SystemInstruction(new List<Part>
        {
            new(instruction.ToString())
        });
    }

    private string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" or ".oga" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }
}