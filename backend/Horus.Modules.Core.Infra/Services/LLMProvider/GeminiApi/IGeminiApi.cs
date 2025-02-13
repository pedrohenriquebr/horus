using System.Text.Json;
using Refit;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;

public record FileData(string MimeType, string FileUri); // Para áudio longo

public record GenerateContentRequest(
    List<Content> Contents,
    List<Tool>? Tools = null,
    List<SafetySetting>? SafetySettings = null,
    GenerationConfig? GenerationConfig = null,
    SystemInstruction? SystemInstruction = null,
    ToolConfig? tool_config = null);

public record ToolConfig(
    FunctionCallingConfig? function_calling_config = null
);

public record FunctionCallingConfig(string mode);

public record Content(string? Role, List<Part> Parts);

public record Part(
    string? Text = null,
    InlineData? InlineData = null,
    FileData? FileData = null,
    FunctionCall? FunctionCall = null);

public record InlineData(string MimeType, string Data);

public record SafetySetting(string Category, string Threshold);

public record GenerationConfig(
    List<string>? StopSequences = null,
    float? Temperature = null,
    int? MaxOutputTokens = null,
    float? TopP = null,
    int? TopK = null);

public record SystemInstruction(List<Part> Parts);

public record Tool(List<FunctionDeclaration>? function_declarations = null);

public record FunctionDeclaration(string Name, string Description, ParameterDefinition Parameters);

public record PropertyDefinition(
    string Type,
    string Description);

public record ParameterDefinition(
    string Type,
    Dictionary<string, PropertyDefinition> Properties,
    List<string> Required);

public record FunctionCall(
    string Name,
    JsonElement Args);

public record FunctionResponse(
    string Name,
    string Response);

public record GenerateContentResponse(List<Candidate> Candidates);

public record Candidate(Content Content);

// Interface Refit
[Headers("Content-Type: application/json")]
public interface IGeminiApi
{
    [Post("/v1beta/models/{model}:generateContent?key={apiKey}")]
    Task<GenerateContentResponse> GenerateContentAsync(
        string model,
        string apiKey,
        [Body] GenerateContentRequest request);

    [Post("/v1beta/models/{model}:streamGenerateContent?alt=sse&key={apiKey}")]
    Task<Stream> StreamGenerateContentAsync(
        string model,
        string apiKey,
        [Body] GenerateContentRequest request);
}