using Horus.Modules.Core.Application.Services;
using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Infra.Services.RAG;

public class DefaultMemoryProvider : IMemoryProvider
{
    private readonly ILogger<DefaultMemoryProvider> _logger;
    private readonly IRagService _ragService;

    public DefaultMemoryProvider(
        IRagService ragService,
        ILogger<DefaultMemoryProvider> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    public async Task<string> GetContextAsync(string query)
    {
        try
        {
            var similarDocs = await _ragService.SearchSimilarAsync(query);
            var memoryDocs = similarDocs
                .Where(doc => doc.Metadata.GetValueOrDefault("type")?.ToString() == "memory")
                .OrderByDescending(doc => doc.CreatedAt)
                .Take(5);

            if (!memoryDocs.Any()) return string.Empty;

            var memories = memoryDocs.Select(doc => doc.Content);
            return string.Join("\n", memories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory context: {message}", ex.Message);
            return string.Empty;
        }
    }

    public async Task<IEnumerable<MemoryItem>> GetMemoriesAsync(Dictionary<string, string> userInfo)
    {
        try
        {
            var userId = userInfo.GetValueOrDefault("id");
            if (string.IsNullOrEmpty(userId)) return Enumerable.Empty<MemoryItem>();

            var docs = await _ragService.GetAllMemoriesByUserId(userId);
            return docs
                .OrderBy(doc => doc.CreatedAt)
                .Select(doc =>
                {
                    var mt = doc.Metadata?.GetValueOrDefault("metadata") is Dictionary<string, object> metadata
                        ? metadata
                        : new Dictionary<string, object>();

                    return new MemoryItem(
                        doc.Content,
                        doc.CreatedAt,
                        mt?.GetValueOrDefault("source")?.ToString() ?? "unknown",
                        mt
                    );
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memories: {message}", ex.Message);
            return Enumerable.Empty<MemoryItem>();
        }
    }

    public async Task PurgeOldMemoriesAsync(Dictionary<string, object> userInfo, int keepCount)
    {
        //por enquanto não implementar
        throw new NotImplementedException();
    }

    public async Task ClearMemoriesAsync(Dictionary<string, string> userInfo)
    {
        var userId = userInfo.GetValueOrDefault("id");
        if (string.IsNullOrEmpty(userId))
            return;

        await _ragService.ClearAllMemoriesByUserIdAsync(userId);
    }

    public async Task<bool> StoreMemoryAsync(MemoryItem item, Dictionary<string, string> userInfo)
    {
        try
        {
            var userId = userInfo.GetValueOrDefault("id");
            if (string.IsNullOrEmpty(userId)) return false;

            var metadata = new Dictionary<string, object>
            {
                ["type"] = "memory",
                ["user_id"] = userId,
                ["timestamp"] = item.CreatedAt.ToString("o"),
                ["source"] = item.Source,
                ["metadata"] = item.Metadata
            };

            await _ragService.AddDocumentAsync(item.Content, metadata);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing memory: {message}", ex.Message);
            return false;
        }
    }

    public async Task UpdateWorkingMemoryAsync(string query, Dictionary<string, string> userInfo)
    {
        var userId = userInfo.GetValueOrDefault("id");
        if (string.IsNullOrEmpty(userId))
            return;
        try
        {
            var metadata = new Dictionary<string, object>
            {
                ["type"] = "working_memory",
                ["user_id"] = userId,
                ["timestamp"] = DateTime.UtcNow.ToString("o")
            };
            await _ragService.AddDocumentAsync(query, metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating working memory for user {UserId}", userId);
        }
    }
}