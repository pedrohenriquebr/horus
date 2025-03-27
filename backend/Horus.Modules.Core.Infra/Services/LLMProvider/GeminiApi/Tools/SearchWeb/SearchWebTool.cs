using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.LLM;
using Horus.Modules.Core.Infra.Services.LLMProvider.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

public class SearchWebTool : ITool
{
    private readonly ILogger<SearchWebTool> _logger;
    private readonly HttpClient _httpClient;
    private readonly OllamaProvider _ollamaProvider;
    private readonly ITextSummarizer _textSummarizer;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10);
    private readonly IWebPageCacheProvider _webPageCacheProvider;
    private readonly ParallelOptions _parallelOptions = new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };

    public SearchWebTool(
        ILogger<SearchWebTool> logger,
        IServiceProvider serviceProvider,
        ITextSummarizer textSummarizer,
        IWebPageCacheProvider webPageCacheProvider)
    {
        _logger = logger;
        _textSummarizer = textSummarizer;
        _webPageCacheProvider = webPageCacheProvider;
        _httpClient = new HttpClient();
        var scope = serviceProvider.CreateScope();
        _ollamaProvider = scope.ServiceProvider.GetRequiredService<OllamaProvider>();

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public string Name => "search";
    public string Description => "Searches for information on the web and returns results with sources";

    public async Task<object> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            var searchParams = new SearchParameters()
            {
                Query = parameters.TryGetValue("Query", out var query) ? query.ToString() : null,
                NumResults = parameters.TryGetValue("NumResults", out var numResults) && int.TryParse(numResults.ToString(), out var num) ? num : 5
            };

            if (string.IsNullOrEmpty(searchParams.Query))
            {
                throw new ArgumentException("Query parameter is required");
            }

            _logger.LogInformation("Starting web search | Query: {Query} | RequestedResults: {NumResults}", 
                searchParams.Query, searchParams.NumResults);

            var searchQuery = HttpUtility.UrlEncode(searchParams.Query);
            var url = $"https://html.duckduckgo.com/html/?q={searchQuery}";
            
            var searchResponse = await _httpClient.GetAsync(url);
            searchResponse.EnsureSuccessStatusCode();

            var content = await searchResponse.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var results = new ConcurrentBag<SearchResult>();
            var searchResults = doc.DocumentNode.SelectNodes("//div[contains(@class, 'result')]");

            if (searchResults != null)
            {
                _logger.LogInformation("Found {Count} initial search results", searchResults.Count);
                
                var processingTasks = searchResults
                    .Take(searchParams.NumResults ?? 5)
                    .Select(async searchResult =>
                    {
                        var titleNode = searchResult.SelectSingleNode(".//a[contains(@class, 'result__a')]");
                        var snippetNode = searchResult.SelectSingleNode(".//a[contains(@class, 'result__snippet')]");

                        if (titleNode != null)
                        {
                            var result = new SearchResult
                            {
                                Title = titleNode.InnerText.Trim(),
                                Link = CleanupUrl(titleNode.GetAttributeValue("href", string.Empty)),
                                Snippet = snippetNode?.InnerText.Trim() ?? string.Empty
                            };

                            await ProcessWebPage(result, 0);
                            results.Add(result);
                        }
                    });

                await Task.WhenAll(processingTasks);
            }

            var finalSummaryPrompt = new StringBuilder();
            finalSummaryPrompt.AppendLine($"Based on the following summaries, provide a comprehensive answer to the query: '{searchParams.Query}'");
            finalSummaryPrompt.AppendLine("\nSource summaries:");

            foreach (var result in results)
            {
                finalSummaryPrompt.AppendLine($"\nTitle: {result.Title}");
                finalSummaryPrompt.AppendLine($"Summary: {result.Summary}");
            }

            var systemInstruction = "You are a helpful assistant that provides accurate and comprehensive summaries based on multiple sources. Focus on answering the user's query directly while maintaining accuracy and citing sources when relevant.";

            var finalSummary = await _textSummarizer.SummarizeTextAsync(finalSummaryPrompt.ToString(), systemInstruction);

            timer.Stop();
            _logger.LogInformation(
                "Search completed | Query: {Query} | ProcessedResults: {ResultCount} | TotalTime: {Duration}ms",
                searchParams.Query,
                results.Count,
                timer.ElapsedMilliseconds);

            return new SearchResponse
            {
                Results = results.ToList(),
                FinalSummary = finalSummary
            };
        }
        catch (Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "Search failed | Duration: {Duration}ms", timer.ElapsedMilliseconds);
            return new SearchResponse
            {
                Error = ex.Message,
                Results = new List<SearchResult>()
            };
        }
    }

    private async Task ProcessWebPage(SearchResult result, int depth, CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (!await _webPageCacheProvider.HasPageContentAsync(result.Link))
                {
                    var content = await ExtractContent(result.Link);
                    await _webPageCacheProvider.StorePageContentAsync(result.Link, content);
                    result.ExtractedContent = content;
                    
                    _logger.LogInformation(
                        "New page processed | URL: {Url} | Depth: {Depth} | ContentSize: {Size}bytes",
                        result.Link,
                        depth,
                        content.Length);
                }
                else
                {
                    result.ExtractedContent = await _webPageCacheProvider.GetPageContentAsync(result.Link);
                    _logger.LogInformation("Cache hit for URL: {Url}", result.Link);
                }
                
                if (!await _webPageCacheProvider.HasPageSummaryAsync(result.Link))
                {
                    var summary = await GenerateSummary(result.ExtractedContent);
                    await _webPageCacheProvider.StorePageSummaryAsync(result.Link, summary);
                    result.Summary = summary;
                }
                else
                {
                    result.Summary = await _webPageCacheProvider.GetPageSummaryAsync(result.Link);
                }

                if (depth < 2)
                {
                    var web = new HtmlWeb();
                    var doc = await web.LoadFromWebAsync(result.Link, cancellationToken);
                    var links = doc.DocumentNode.SelectNodes("//a[@href]")?.Take(3).ToList();

                    if (links != null)
                    {
                        var linkedProcessingTasks = links.Select(async link =>
                        {
                            var href = link.GetAttributeValue("href", "");
                            if (Uri.TryCreate(new Uri(result.Link), href, out var absoluteUri))
                            {
                                var linkedResult = new SearchResult
                                {
                                    Link = absoluteUri.ToString(),
                                    Title = link.InnerText.Trim()
                                };

                                await ProcessWebPage(linkedResult, depth + 1, cancellationToken);
                            }
                        });

                        await Task.WhenAll(linkedProcessingTasks);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing web page: {Url}", result.Link);
        }
        finally
        {
            timer.Stop();
            LogProcessingMetrics("PageProcessing", result.Link, timer);
        }
    }

    private async Task<string> ExtractContent(string url)
    {
        var timer = Stopwatch.StartNew();
        var contentBuilder = new StringBuilder();
        
        try 
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);
            var mainContent = doc.DocumentNode.SelectNodes("//article|//main|//div[@class='content']|//div[@role='main']|//p");

            if (mainContent != null)
            {
                await Task.Run(() => 
                {
                    Parallel.ForEach(mainContent, _parallelOptions, node =>
                    {
                        contentBuilder.AppendLine(node.InnerText.Trim());
                    });
                });
            }

            _logger.LogInformation(
                "Content extracted | URL: {Url} | Size: {ContentSize}bytes | Nodes: {NodeCount}",
                url,
                contentBuilder.Length,
                mainContent?.Count ?? 0);

            return contentBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content extraction failed | URL: {Url}", url);
            throw;
        }
        finally
        {
            timer.Stop();
            LogProcessingMetrics("ContentExtraction", url, timer);
        }
    }

    private async Task<string> GenerateSummary(string extractedContent)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            var systemInstruction = new Dictionary<string, object>
            {
                { "text", "You are a helpful assistant that provides concise and accurate summaries of web content. Focus on the main points and key information." }
            };

            var summary = await _ollamaProvider.GenerateTextAsync(
                $"Summarize the following web content:\n{extractedContent}",
                systemInstruction);

            _logger.LogInformation(
                "Summary generated | ContentSize: {InputSize}bytes | SummarySize: {OutputSize}bytes",
                extractedContent.Length,
                summary.Length);

            return summary;
        }
        finally
        {
            timer.Stop();
            LogProcessingMetrics("SummaryGeneration", "N/A", timer);
        }
    }

    private void LogProcessingMetrics(string operation, string url, Stopwatch timer)
    {
        _logger.LogInformation(
            "Operation: {Operation} | URL: {Url} | Duration: {Duration}ms | Thread: {ThreadId}",
            operation,
            url,
            timer.ElapsedMilliseconds,
            Environment.CurrentManagedThreadId);
    }

    private string CleanupUrl(string url)
    {
        var match = Regex.Match(url, @"uddg=([^&]+)");
        return match.Success ? HttpUtility.UrlDecode(match.Groups[1].Value) : url;
    }
}