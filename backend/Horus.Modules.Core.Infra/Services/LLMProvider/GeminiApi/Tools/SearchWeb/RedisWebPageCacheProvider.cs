using System.Security.Cryptography;
using System.Text;
using Horus.Modules.Shared.Contracts.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

public class RedisWebPageCacheProvider : IWebPageCacheProvider
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _contentKeyPrefix;
    private readonly string _summaryKeyPrefix;
    private readonly TimeSpan _defaultTtl;

    public RedisWebPageCacheProvider(
        IConnectionMultiplexer redis, 
        IOptions<RedisConfig> options)
    {
        _redis = redis;
        _contentKeyPrefix = $"{options.Value.WebPageCacheKey}{options.Value.KeyDelimiter}content:";
        _summaryKeyPrefix = $"{options.Value.WebPageCacheKey}{options.Value.KeyDelimiter}summary:";
        _defaultTtl = TimeSpan.FromHours(24);
    }

    public async Task<string?> GetPageContentAsync(string url)
    {
        var key = BuildContentKey(url);
        return await _redis.GetDatabase().StringGetAsync(key);
    }

    public async Task<string?> GetPageSummaryAsync(string url)
    {
        var key = BuildSummaryKey(url);
        return await _redis.GetDatabase().StringGetAsync(key);
    }

    public async Task StorePageContentAsync(string url, string content, TimeSpan? ttl = null)
    {
        var key = BuildContentKey(url);
        await _redis.GetDatabase().StringSetAsync(
            key,
            content,
            ttl ?? _defaultTtl
        );
    }

    public async Task StorePageSummaryAsync(string url, string summary, TimeSpan? ttl = null)
    {
        var key = BuildSummaryKey(url);
        await _redis.GetDatabase().StringSetAsync(
            key,
            summary,
            ttl ?? _defaultTtl
        );
    }

    public async Task<bool> HasPageContentAsync(string url)
    {
        var key = BuildContentKey(url);
        return await _redis.GetDatabase().KeyExistsAsync(key);
    }

    public async Task<bool> HasPageSummaryAsync(string url)
    {
        var key = BuildSummaryKey(url);
        return await _redis.GetDatabase().KeyExistsAsync(key);
    }

    public async Task InvalidatePageCacheAsync(string url)
    {
        await _redis.GetDatabase().KeyDeleteAsync(BuildContentKey(url));
        await _redis.GetDatabase().KeyDeleteAsync(BuildSummaryKey(url));
    }

    private string BuildContentKey(string url) => 
        $"{_contentKeyPrefix}{CreateUrlHash(url)}";

    private string BuildSummaryKey(string url) => 
        $"{_summaryKeyPrefix}{CreateUrlHash(url)}";

    private static string CreateUrlHash(string url) => 
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
}