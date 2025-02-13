namespace Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;

public class Message
{
    public string Role { get; set; }
    public string Content { get; set; }
    public List<string>? Images { get; set; }
}