namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

public class SearchResult
{
    public string Title { get; set; }
    public string Link { get; set; }
    public string Snippet { get; set; }
    public string ExtractedContent { get; set; }
    public string Summary { get; set; }
}