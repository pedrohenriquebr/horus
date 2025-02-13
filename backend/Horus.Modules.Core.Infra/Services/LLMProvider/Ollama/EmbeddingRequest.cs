namespace Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;

public class EmbeddingRequest
{
    public string Model { get; set; }
    public string Prompt { get; set; }
}