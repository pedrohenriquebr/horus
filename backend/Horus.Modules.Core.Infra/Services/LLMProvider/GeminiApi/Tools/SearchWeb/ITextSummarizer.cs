namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

public interface ITextSummarizer
{
    Task<string> SummarizeTextAsync(string text, string systemInstruction);
}