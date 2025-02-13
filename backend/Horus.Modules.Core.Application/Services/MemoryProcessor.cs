using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace Horus.Modules.Core.Application.Services;

public class MemoryProcessor(IMemoryCache memoryCache, IMemoryProvider memoryProvider) : IMemoryProcessor
{
    public async Task<string> ProcessMemoryTags(string response, Dictionary<string, string> userInfo)
    {
        // Match both <store_memory> and <memory> tags
        var memoryMatches = Regex.Matches(response, "<(store_memory|memory)>(.*?)</\\1>");

        if (memoryMatches.Count > 0 && userInfo.TryGetValue("id", out var value))
        {
            var memoryUserInfo = userInfo;
            var sanitizedMemories = memoryMatches
                .Select(m => SanitizeMemoryContent(m.Groups[2].Value)) // Group 2 captures the memory content
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            var cacheKey = $"user_memories_{value}";
            if (!memoryCache.TryGetValue(cacheKey, out List<string>? cachedMemories))
            {
                cachedMemories = (await memoryProvider.GetMemoriesAsync(userInfo))
                    .Select(x => x.Content).ToList();
                memoryCache.Set(cacheKey, cachedMemories, TimeSpan.FromMinutes(30));
            }

            foreach (var memory in sanitizedMemories)
            {
                var memoryItem = new MemoryItem(
                    memory,
                    DateTime.UtcNow,
                    "LLM-tagged",
                    new Dictionary<string, object>
                    {
                        ["version"] = "1.0"
                    }
                );
                await memoryProvider.StoreMemoryAsync(memoryItem, memoryUserInfo);
            }

            // Remove both <store_memory> and <memory> tags
            response = Regex.Replace(response, "<(store_memory|memory)>(.*?)</\\1>", "");
        }

        return response;
    }


    private string SanitizeMemoryContent(string input)
    {
        return Regex.Replace(input, @"<[^>]+>|&[^;]+;", string.Empty)
            .Trim()
            .Replace("\n", " ")
            .Replace("\r", " ");
    }
}