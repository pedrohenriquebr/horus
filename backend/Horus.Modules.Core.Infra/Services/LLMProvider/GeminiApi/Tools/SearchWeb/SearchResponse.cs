namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

public class SearchResponse
{
    public List<SearchResult> Results { get; set; }
    public string Error { get; set; }
    public string FinalSummary { get; set; }
}