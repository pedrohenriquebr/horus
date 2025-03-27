namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

public interface IWebPageCacheProvider
{
    Task<string?> GetPageContentAsync(string url);
    Task<string?> GetPageSummaryAsync(string url);
    Task StorePageContentAsync(string url, string content, TimeSpan? ttl = null);
    Task StorePageSummaryAsync(string url, string summary, TimeSpan? ttl = null);
    Task<bool> HasPageContentAsync(string url);
    Task<bool> HasPageSummaryAsync(string url);
    Task InvalidatePageCacheAsync(string url);
}