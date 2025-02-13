using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Horus.Modules.Shared.Contracts.Events;
using Horus.Modules.Shared.Contracts.Services;
using StackExchange.Redis;

namespace LuzInga.Modules.Shared.Infrastructure.Services;

public class AuditLogger : IAuditLogger
{
    private readonly RedisKey _fullKey;

    private readonly IConnectionMultiplexer _redis;
    private JsonSerializerOptions _serializerOptions;

    public AuditLogger(IConnectionMultiplexer redis, RedisKey fullkey)
    {
        _redis = redis;
        _fullKey = fullkey;
        _serializerOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task LogRecent(ApplicationAccessedEvent request)
    {
        var db = _redis.GetDatabase();
        await db.ListLeftPushAsync(_fullKey,
            new StringBuilder()
                .Append(request.Datetime)
                .Append("     ")
                .Append("Recent")
                .Append("%")
                .Append(request.Url)
                .Append("%")
                .Append(request.Username)
                .Append("%")
                .Append(request.Method)
                .Append("%")
                .Append(request.RemoteIpAddress)
                .ToString());
        await db.ListTrimAsync(_fullKey, 0, 99);
    }

    public async Task LogRecent(string request, object requestData, object? responseData)
    {
        var db = _redis.GetDatabase();
        await db.ListLeftPushAsync(_fullKey, JsonSerializer.Serialize(new
        {
            Request = request,
            RequestData = requestData,
            ResponseData = responseData
        }));
        await db.ListTrimAsync(_fullKey, 0, 99);
    }
}