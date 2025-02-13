namespace Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;

public class GenerateRequest
{
    public string Model { get; set; }
    public bool Stream { get; set; } = false;
    public string Prompt { get; set; }
    public Dictionary<string, object>? Options { get; set; }
    public List<Message>? Messages { get; set; }
    public string? System { get; set; }
    public string? Template { get; set; }
    public string? Context { get; set; }
}