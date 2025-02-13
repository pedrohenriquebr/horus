namespace Horus.Modules.Core.Infra.Services.RAG;

public class SupabaseOptions
{
    public string HuggingFaceApiKey { get; set; } = string.Empty; //HuggingFaceApiKey
    public string HuggingFaceModelName { get; set; } = string.Empty; //HuggingFaceModelName
}