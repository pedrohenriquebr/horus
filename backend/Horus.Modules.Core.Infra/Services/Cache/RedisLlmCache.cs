using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Horus.Modules.Core.Infra.Services.Cache;

public class RedisLlmCache : ILlmCache
{
    private const string KeyPrefix = "llm:response:";
    private readonly ILogger<RedisLlmCache> _logger;
    private readonly IConnectionMultiplexer _redis;

    public RedisLlmCache(
        IConnectionMultiplexer redis,
        ILogger<RedisLlmCache> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string?> GetResponseAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var redisKey = GetRedisKey(key);
            return await db.StringGetAsync(redisKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LLM response from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetResponseAsync(string key, string response, TimeSpan? expiry = null)
    {
        try
        {
            var db = _redis.GetDatabase();
            var redisKey = GetRedisKey(key);
            await db.StringSetAsync(redisKey, response, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting LLM response in cache for key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var redisKey = GetRedisKey(key);
            return await db.KeyExistsAsync(redisKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking LLM response existence in cache for key: {Key}", key);
            return false;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var db = _redis.GetDatabase();
            var redisKey = GetRedisKey(key);
            await db.KeyDeleteAsync(redisKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing LLM response from cache for key: {Key}", key);
        }
    }

    private static string GetRedisKey(string key)
    {
        return $"{KeyPrefix}{key}";
    }
}