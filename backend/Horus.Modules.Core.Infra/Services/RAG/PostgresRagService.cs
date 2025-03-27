using Horus.Modules.Core.Application.Events;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Events;
using Horus.Modules.Core.Domain.Factories;
using Horus.Modules.Core.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;

namespace Horus.Modules.Core.Infra.Services.RAG;

public class PostgresRagService : IRagService
{
    private readonly IDistributedCache _cache;
    private readonly IMediator _mediator;
    private readonly IDocumentFactory _documentFactory;
    private readonly IOptions<RagOptions> _options;
    private readonly IDocumentsRepository _documentsRepository;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<PostgresRagService> _logger;

    public PostgresRagService(
        IDocumentsRepository documentsRepository,
        IEmbeddingService embeddings,
        IDistributedCache cache,
        IMediator mediator,
        ILogger<PostgresRagService> logger,
        IDocumentFactory documentFactory,
        IOptions<RagOptions> options)
    {
        _documentsRepository = documentsRepository;
        _embeddings = embeddings;
        _cache = cache;
        _mediator = mediator;
        _documentFactory = documentFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<Document> AddDocumentForSearchResultAsync(string content, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var existingDocs = await _documentsRepository.FindByContentAsync(content);
            if (existingDocs != null)
            {
                _logger.LogInformation("Document already exists, skipping: {content}", content[..100]);
                return existingDocs!;
            }

            
            //TODO: Remove this
            var newDoc = _documentFactory.CreateMemory(content);
            if(metadata is not null)
                newDoc.SetMetadata(metadata);
            await _documentsRepository.InsertAsync(newDoc);
            
            return newDoc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document: {message}", ex.Message);
            throw;
        }
    }

    public async Task<Document> AddDocumentForMemoryAsync(string content, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var existingDocs = await _documentsRepository.FindByContentAsync(content);
            if (existingDocs != null)
            {
                _logger.LogInformation("Document already exists, skipping: {content}", content[..100]);
                return existingDocs!;
            }

            var newDoc = _documentFactory.CreateMemory(content);
            if(metadata is not null)
                newDoc.SetMetadata(metadata);
            await _documentsRepository.InsertAsync(newDoc);
            
            return newDoc;
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

            var document = await AddDocumentForSearchResultAsync(content, metadata);

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
            var results = await _documentsRepository.MatchDocumentsAsync(queryEmbedding, limit, _options.Value.SearchThresold);
            var searchResults = results
                .Where(r => r.Metadata.GetValueOrDefault("type")?.ToString() == "search_result")
                .Select(r => new SearchResult
                {
                    Id = r.Id.ToString(),
                    Url = r.Metadata.GetValueOrDefault("url")?.ToString() ?? string.Empty,
                    Content = r.ProcessedContent,
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
                    Content = r.ProcessedContent,
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

    public async Task<IEnumerable<Document>> SearchSimilarAsync(string query)
    {
        try
        {
            // Get embedding for query
            var queryEmbedding = await _embeddings.GetEmbeddingAsync(query);

            // Search for similar documents
            var results = await _documentsRepository.MatchDocumentsAsync(queryEmbedding, _options.Value.SearchLimit, _options.Value.SearchThresold);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching similar documents: {message}", ex.Message);
            return Enumerable.Empty<Document>();
        }
    }

    public async Task<IEnumerable<HybridSearchResult>> SearchSimilarHybridAsync(string requestPrompt, Guid? userId = null)
    {
        try
        {
            // Get embedding for query
            var queryEmbedding = await _embeddings.GetEmbeddingAsync(requestPrompt);

            // Search for similar documents
            var results = await _documentsRepository.MatchDocumentsHybridAsync(queryEmbedding, requestPrompt, userId,_options.Value.SearchLimit, _options.Value.SearchThresold );
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching similar documents: {message}", ex.Message);
            return Enumerable.Empty<HybridSearchResult>();
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

    public async Task DeleteDocumentAsync(Guid id)
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="document"></param>
    public async Task IngestAsync(Document document)
    {
        try
        {
            var hash = document.Checksum;
            var existingDocs = await _documentsRepository.AnyAsync<Document>(d => d.Checksum == hash);
            if (existingDocs)
            {
                _logger.LogInformation("Document already exists, skipping: {content}", document.RawContent[..100]);
                return;
            }
            
            await _documentsRepository.InsertAsync(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document: {message}", ex.Message);
            throw;
        }
    }

    public async Task<string> GetContextAsync(string query)
    {
        try
        {
            // First try to find exact matches
            var exactMatches = await _documentsRepository.GetAllMemoriesByUserId($"content LIKE '%{query}%'");
            if (exactMatches.Any()) return exactMatches.First().ProcessedContent;

            var similarDocs = await SearchSimilarAsync(query);
            if (!similarDocs.Any()) return string.Empty;

            var contextParts = similarDocs
                .Where(doc => doc.Metadata.GetValueOrDefault("type")?.ToString() != "memory")
                .Select(doc =>
                {
                    var similarity = doc.Metadata.GetValueOrDefault("similarity") is float sim ? sim : 0f;
                    return $"[Relevância: {similarity:F2}] {doc.ProcessedContent}";
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