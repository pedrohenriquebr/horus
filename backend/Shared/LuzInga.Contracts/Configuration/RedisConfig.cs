namespace Horus.Modules.Shared.Contracts.Configuration;

public class RedisConfig
{
    public string ApplicationPrefixKey { get; set; }
    public string KeyDelimiter { get; set; }
    public string AuditListKey { get; set; }
    public string ChatHistoryKey { get; set; }
    public string WebPageCacheKey { get; set; } = "webpage";
}