using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Application.Services;

public interface IRagService
{
    Task<Document> AddDocumentForSearchResultAsync(string content, Dictionary<string, object>? metadata = null);
    Task<Document> AddDocumentForMemoryAsync(string content, Dictionary<string, object>? metadata = null);
    Task<SearchResult> AddSearchResultAsync(string url, string content, string? summary = null);
    Task<IEnumerable<SearchResult>> GetSearchResultsAsync(string query, int limit = 5);
    Task<IEnumerable<Document>> SearchSimilarAsync(string query);
    Task<IEnumerable<HybridSearchResult>> SearchSimilarHybridAsync(string requestPrompt, Guid? userId = null);
    Task<IEnumerable<Document>> GetAllMemoriesByUserId(string userId, int limit = 5);
    Task ClearAllMemoriesByUserIdAsync(string userId);
    Task DeleteDocumentAsync(Guid id);
    Task IngestAsync(Document document);
    
}