namespace Horus.Modules.Core.Application.Services;

public interface IMemoryProvider
{
    Task<bool> StoreMemoryAsync(MemoryItem item, Dictionary<string, string> userInfo);
    Task<IEnumerable<MemoryItem>> GetMemoriesAsync(Dictionary<string, string> userInfo);
    Task UpdateWorkingMemoryAsync(string query, Dictionary<string, string> userInfo);
    Task<string> GetContextAsync(string query);
    Task PurgeOldMemoriesAsync(Dictionary<string, object> userInfo, int keepCount);
    Task ClearMemoriesAsync(Dictionary<string, string> userInfo);
}