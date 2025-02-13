using System.Text;
using Horus.Modules.Shared.Contracts.Configuration;
using Horus.Modules.Shared.Contracts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LuzInga.Modules.Shared.Infrastructure.Services;

public class RedisKeyFactory : IRedisKeyFactory
{
    private const string CachingPrefix = "Caching";
    private readonly IOptions<RedisConfig> _config;

    public RedisKeyFactory(IOptions<RedisConfig> redisConfig)
    {
        _config = redisConfig;
    }

    public string CreateAuditPrefix()
    {
        return CreateInstanceNamePrefix(_config)
            .Append(_config.Value.AuditListKey)
            .ToString();
    }

    public string CreateCachingKey(PathString path, IQueryCollection query)
    {
        var sb = new StringBuilder();
        sb.Append(CachingPrefix);
        sb.Append(_config.Value.KeyDelimiter);
        sb.Append(path);

        foreach (var item in query)
        {
            sb.Append(_config.Value.KeyDelimiter);
            sb.Append(item.Key);
            sb.Append(_config.Value.KeyDelimiter);
            sb.Append(item.Value);
        }

        return sb.ToString();
    }

    public string CreateGlobalInstancePrefix()
    {
        return CreateInstanceNamePrefix(_config)
            .ToString();
    }

    private static StringBuilder CreateInstanceNamePrefix(IOptions<RedisConfig> redisConfig)
    {
        return new StringBuilder()
            .Append(redisConfig.Value.ApplicationPrefixKey)
            .Append(redisConfig.Value.KeyDelimiter);
    }
}