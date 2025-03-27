using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;
using Horus.Modules.Core.Infra.Services.RateLimiter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

namespace Horus.Modules.Core.Infra.Services.LLMProvider
{
    public class GeminiProvider : ILlmProvider, ITextSummarizer
    {
        private readonly IOptions<GeminiConfig> _configuration;
        private readonly IGeminiApi _geminiApi;
        private readonly ILogger<GeminiProvider> _logger;
        private readonly IRateLimiter _rateLimiter;
        private readonly string _systemInstruction = null;
        private IToolMediator? _toolMediator;

        public GeminiProvider(
            IRateLimiter rateLimiter,
            ILogger<GeminiProvider> logger,
            IOptions<GeminiConfig> configuration,
            IGeminiApi geminiApi)
        {
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _geminiApi = geminiApi ?? throw new ArgumentNullException(nameof(geminiApi));
        }

        public GeminiProvider AddToolMediator(IToolMediator toolMediator)
        {
            _toolMediator = toolMediator;
            return this;
        }

        public async Task<string> GenerateTextAsync(
            string prompt,
            Dictionary<string, object>? systemInstruction = null,
            List<ChatMessage>? chatHistory = null)
        {
            return await GenerateContentAsync(prompt, systemInstruction, chatHistory);
        }

        public async Task<string> GenerateWithImageAsync(
            string imagePath,
            string? prompt = null,
            Dictionary<string, object>? systemInstruction = null,
            List<ChatMessage>? chatHistory = null)
        {
            var mediaParts = CreateMediaParts(imagePath, prompt, GetMimeType(imagePath));
            return await GenerateContentAsync(null, systemInstruction, chatHistory, mediaParts);
        }

        public async Task<string> GenerateWithAudioAsync(
            string audioPath,
            string? prompt = null,
            Dictionary<string, object>? systemInstruction = null,
            List<ChatMessage>? chatHistory = null)
        {
            var mediaParts = CreateMediaParts(audioPath, prompt, GetMimeType(audioPath));
            return await GenerateContentAsync(null, systemInstruction, chatHistory, mediaParts);
        }

        private async Task<string> GenerateContentAsync(
            string? prompt,
            Dictionary<string, object>? systemInstruction,
            List<ChatMessage>? chatHistory,
            List<Part>? mediaParts = null)
        {
            try
            {
                if (!_rateLimiter.TryAcquire())
                {
                    _logger.LogWarning("Rate limit exceeded, waiting...");
                    await _rateLimiter.WaitAsync();
                }

                var contents = BuildContents(chatHistory, prompt, mediaParts);

                var request = new GenerateContentRequest(
                    contents,
                    SystemInstruction: BuildSystemInstruction(systemInstruction)
                );

                if (_toolMediator != null)
                    request = request.AddWebSearchTool();

                var response = await _geminiApi.GenerateContentAsync(
                    _configuration.Value.ModelName,
                    _configuration.Value.ApiKey,
                    request);

                if (_toolMediator != null && response.HasFunctionCall())
                {
                    return await response.HandleFunctionCallsAsync(_toolMediator);
                }

                return ExtractResponseText(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content");
                throw;
            }
        }

        private List<Content> BuildContents(
            List<ChatMessage>? chatHistory,
            string? prompt,
            List<Part>? mediaParts)
        {
            var contents = new List<Content>();

            if (chatHistory != null)
            {
                contents.AddRange(chatHistory
                    .OrderBy(d => d.Timestamp)
                    .Select(message => new Content(
                        message.Role,
                        new List<Part>
                        {
                            new(message.Content)
                        })));
            }

            if (mediaParts != null)
            {
                contents.Add(new Content("user", mediaParts));
            }
            else if (!string.IsNullOrEmpty(prompt))
            {
                contents.Add(new Content("user", new List<Part> { new(prompt) }));
            }

            return contents;
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

        private List<Part> CreateMediaParts(string filePath, string? prompt, string mimeType)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var base64Data = Convert.ToBase64String(fileBytes);

            var parts = new List<Part>
            {
                new(InlineData: new InlineData(mimeType, base64Data))
            };

            if (!string.IsNullOrEmpty(prompt))
            {
                parts.Insert(0, new Part(prompt));
            }

            return parts;
        }

        private string ExtractResponseText(GenerateContentResponse response)
        {
            return response.Candidates[0].Content.Parts[0].Text;
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

        public Task<string> SummarizeTextAsync(string text, string systemInstruction = null)
        {
            return GenerateContentAsync(text, new Dictionary<string, object>(){["text"]= systemInstruction}, null);
        }
    }
}