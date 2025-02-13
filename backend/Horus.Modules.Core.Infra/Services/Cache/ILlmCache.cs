namespace Horus.Modules.Core.Infra.Services.Cache;

public interface ILlmCache
{
    Task<string?> GetResponseAsync(string key);
    Task SetResponseAsync(string key, string response, TimeSpan? expiry = null);
    Task<bool> ExistsAsync(string key);
    Task RemoveAsync(string key);
}