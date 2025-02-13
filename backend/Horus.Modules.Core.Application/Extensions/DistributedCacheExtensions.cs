using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Horus.Modules.Core.Application.Extensions;

public static class DistributedCacheExtensions
{
    private static readonly int _defaultExpiretime = 60;

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task SetRecordAsync<T>(this IDistributedCache cache,
        string recordId,
        T data,
        TimeSpan? absExpireTime = null,
        TimeSpan? uExpireTime = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absExpireTime ?? TimeSpan.FromSeconds(_defaultExpiretime),
            SlidingExpiration = uExpireTime
        };

        var jsonData = JsonSerializer.Serialize(data, _serializerOptions);
        await cache.SetStringAsync(recordId, jsonData, options);
    }


    public static async Task SetRequestAsync(this IDistributedCache cache,
        string recordId,
        string data,
        TimeSpan? absExpireTime = null,
        TimeSpan? uExpireTime = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absExpireTime ?? TimeSpan.FromSeconds(_defaultExpiretime),
            SlidingExpiration = uExpireTime
        };

        await cache.SetStringAsync(recordId, data, options);
    }


    public static async Task<T> GetRecordAsync<T>(this IDistributedCache cache, string recordId)
    {
        var jsonData = await cache.GetStringAsync(recordId);

        if (jsonData is null)
            return default;

        return JsonSerializer.Deserialize<T>(jsonData, _serializerOptions);
    }


    public static async Task<T> GetOrAddAsync<T>(this IDistributedCache cache, string recordId, Func<Task<T>> handler)
    {
        var fromCache = await cache.GetRecordAsync<T>(recordId);

        if (fromCache is null)
        {
            var result = await handler.Invoke();
            await cache.SetRecordAsync(recordId, result);
            return result;
        }

        return fromCache;
    }
}