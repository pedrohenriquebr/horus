namespace Horus.Modules.Core.Infra.Services.LLMProvider;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-1.5-flash";
    public double TokensPerSecond { get; set; } = 0.25;
    public int Burst { get; set; } = 5;
}