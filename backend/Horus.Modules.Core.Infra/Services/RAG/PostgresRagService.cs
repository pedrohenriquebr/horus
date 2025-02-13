using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Infra.Services.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Horus.Modules.Core.Infra.Services.RAG;

public class PostgresRagService : IRagService
{
    private readonly IDistributedCache _cache;
    private readonly IDocumentsRepository _documentsRepository;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<PostgresRagService> _logger;

    public PostgresRagService(
        IDocumentsRepository documentsRepository,
        IEmbeddingService embeddings,
        IDistributedCache cache,
        ILogger<PostgresRagService> logger)
    {
        _documentsRepository = documentsRepository;
        _embeddings = embeddings;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Document> AddDocumentAsync(string content, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var existingDocs = await _documentsRepository.FindByContentAsync(content);
            if (existingDocs != null)
            {
                _logger.LogInformation("Document already exists, skipping: {content}", content[..100]);
                return existingDocs!;
            }

            // Generate embedding
            var embedding = await _embeddings.GetEmbeddingAsync(content);

            // Create document with default embedding if empty
            var document = new Document
            {
                Content = content,
                Embedding = new Vector(embedding),
                Metadata = metadata
            };

            // Insert document
            var result = await _documentsRepository.InsertAsync(document);
            _logger.LogInformation("Document added successfully: {id}", result.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document: {message}", ex.Message);
            throw;
        }
    }

    public async Task<SearchResult> AddSearchResultAsync(string url, string content, string? summary = null)
    {
        try
        {
            var metadata = new Dictionary<string, object>
            {
                ["type"] = "search_result",
                ["url"] = url,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["summary"] = summary ?? string.Empty
            };

            var document = await AddDocumentAsync(content, metadata);

            return new SearchResult
            {
                Id = document.Id.ToString(),
                Url = url,
                Content = content,
                Summary = summary,
                CreatedAt = document.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding search result: {message}", ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<SearchResult>> GetSearchResultsAsync(string query, int limit = 5)
    {
        try
        {
            // Generate embedding for query
            var queryEmbedding = await _embeddings.GetEmbeddingAsync(query);

            // Search for similar documents
            var results = await _documentsRepository.MatchDocumentsAsync(queryEmbedding, limit, 0.9f);
            var searchResults = results
                .Where(r => r.Metadata.GetValueOrDefault("type")?.ToString() == "search_result")
                .Select(r => new SearchResult
                {
                    Id = r.Id.ToString(),
                    Url = r.Metadata.GetValueOrDefault("url")?.ToString() ?? string.Empty,
                    Content = r.Content,
                    Summary = r.Metadata.GetValueOrDefault("summary")?.ToString(),
                    Similarity = r.Metadata.GetValueOrDefault("similarity") is float sim ? sim : 0f,
                    CreatedAt = r.CreatedAt
                });

            if (!searchResults.Any())
            {
                // Fallback to recent results if no similar documents found
                var recentDocs =
                    await _documentsRepository.GetAllMemoriesByUserId(
                        "metadata->>'type'='search_result' ORDER BY created_at DESC LIMIT " + limit);
                searchResults = recentDocs.Select(r => new SearchResult
                {
                    Id = r.Id.ToString(),
                    Url = r.Metadata.GetValueOrDefault("url")?.ToString() ?? string.Empty,
                    Content = r.Content,
                    Summary = r.Metadata.GetValueOrDefault("summary")?.ToString(),
                    CreatedAt = r.CreatedAt
                });
            }

            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search results: {message}", ex.Message);
            return Enumerable.Empty<SearchResult>();
        }
    }

    public async Task<IEnumerable<Document>> SearchSimilarAsync(string query, int limit = 5)
    {
        try
        {
            // Get embedding for query
            var queryEmbedding = await _embeddings.GetEmbeddingAsync(query);

            // Search for similar documents
            var results = await _documentsRepository.MatchDocumentsAsync(queryEmbedding, limit, 0.9f);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching similar documents: {message}", ex.Message);
            return Enumerable.Empty<Document>();
        }
    }

    public async Task<IEnumerable<Document>> GetAllMemoriesByUserId(string userId, int limit = 5)
    {
        return await _documentsRepository.GetAllMemoriesByUserId(userId, limit);
    }

    public async Task ClearAllMemoriesByUserIdAsync(string userId)
    {
        await _documentsRepository.DeleteAllDocumentsByUserId(userId, DocumentType.Memory);
    }

    public async Task DeleteDocumentAsync(string id)
    {
        try
        {
            await _documentsRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}: {Message}", id, ex.Message);
            throw;
        }
    }

    public async Task<string> GetContextAsync(string query)
    {
        try
        {
            // First try to find exact matches
            var exactMatches = await _documentsRepository.GetAllMemoriesByUserId($"content LIKE '%{query}%'");
            if (exactMatches.Any()) return exactMatches.First().Content;

            var similarDocs = await SearchSimilarAsync(query);
            if (!similarDocs.Any()) return string.Empty;

            var contextParts = similarDocs
                .Where(doc => doc.Metadata.GetValueOrDefault("type")?.ToString() != "memory")
                .Select(doc =>
                {
                    var similarity = doc.Metadata.GetValueOrDefault("similarity") is float sim ? sim : 0f;
                    return $"[Relevância: {similarity:F2}] {doc.Content}";
                });

            _logger.LogDebug("Building context: Found {count} similar documents for query: {query}",
                contextParts.Count(), query);
            return string.Join("\n\n", contextParts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting context: {message}", ex.Message);
            return string.Empty;
        }
    }
}