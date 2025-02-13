using Horus.Modules.Core.Domain.LLM;

namespace Horus.Modules.Core.Application.Services;

public interface ILlmProvider
{
    Task<string> GenerateTextAsync(
        string prompt,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null);

    Task<string> GenerateWithImageAsync(
        string imagePath,
        string? prompt = null,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null);

    Task<string> GenerateWithAudioAsync(
        string audioPath,
        string? prompt = null,
        Dictionary<string, object>? systemInstruction = null,
        List<ChatMessage>? chatHistory = null);
}