namespace Horus.Modules.Core.Domain.Search;

public record SearchResult
{
    public string Url { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Rank { get; init; } = 0;
    public string? Summary { get; init; } = string.Empty;
}

public interface ISearchProvider
{
    Task<IEnumerable<SearchResult>> SearchAsync(string query, int numResults = 5);
    Task<string> SummarizeResultsAsync(string query, IEnumerable<SearchResult> results);
}