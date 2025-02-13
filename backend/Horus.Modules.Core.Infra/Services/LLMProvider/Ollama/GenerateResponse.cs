using System.Text.Json.Serialization;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;

public class GenerateResponse
{
    public string Model { get; set; }

    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

    public bool Done { get; set; }
    public string? Response { get; set; }
    public Message? Message { get; set; }

    [JsonPropertyName("done_reason")] public string? DoneReason { get; set; }

    [JsonPropertyName("total_duration")] public long TotalDuration { get; set; }

    [JsonPropertyName("load_duration")] public long LoadDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int? PromptEvalCount { get; set; }

    [JsonPropertyName("prompt_eval_duration")]
    public long? PromptEvalDuration { get; set; }

    [JsonPropertyName("eval_count")] public int? EvalCount { get; set; }

    [JsonPropertyName("eval_duration")] public long? EvalDuration { get; set; }

    [JsonPropertyName("context")] public int[]? Context { get; set; }
}