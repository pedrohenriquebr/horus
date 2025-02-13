# Technical Specification - Phase 2 Migration

## Overview

This document details the technical specifications for migrating core features from the legacy Python codebase to the
new .NET Core solution.

## Table of Contents

1. [LLM Integration](#llm-integration)
2. [Memory & Storage](#memory--storage)
3. [Tools & Utilities](#tools--utilities)
4. [Implementation Notes](#implementation-notes)

## LLM Integration

### Source Files

- `/legacy/src/core/llm/base.py`
- `/legacy/src/core/llm/providers/gemini.py`
- `/legacy/src/core/llm/tools.py`

### Target Implementation

#### 1. Core Interfaces

Location: `/backend/LuzInga.Domain/LLM/`

```csharp
public interface ILLMProvider
{
    Task<string> GenerateTextAsync(string prompt, Dictionary<string, object>? systemInstruction = null);
    Task<string> GenerateWithImageAsync(string imagePath, string prompt, Dictionary<string, object>? systemInstruction = null);
    Task<string> GenerateWithAudioAsync(string audioPath, string? prompt = null, Dictionary<string, object>? systemInstruction = null);
}

public interface IMemoryProvider
{
    Task<bool> StoreMemoryAsync(string text, Dictionary<string, object> userInfo);
    Task<IEnumerable<string>> GetMemoriesAsync(Dictionary<string, object> userInfo);
    Task UpdateWorkingMemoryAsync(string query, Dictionary<string, object> userInfo);
    Task<string> GetContextAsync(string query);
}

public interface IChatHistoryProvider 
{
    Task StoreMessageAsync(string role, string content, Dictionary<string, object> userInfo);
    Task<IEnumerable<ChatMessage>> GetHistoryAsync(Dictionary<string, object> userInfo);
}
```

#### 2. Gemini Implementation

Location: `/backend/LuzInga.Infrastructure/LLM/Providers/`

```csharp
public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-1.5-flash";
    public double TokensPerSecond { get; set; } = 0.25;
    public int Burst { get; set; } = 5;
}

public class GeminiProvider : ILLMProvider
{
    private readonly Google.Ai.Generative.V1.GenerativeClient _client;
    private readonly IRateLimiter _rateLimiter;
    private readonly IToolMediator _toolMediator;
    private readonly ILogger<GeminiProvider> _logger;

    public GeminiProvider(
        IOptions<GeminiOptions> options,
        IRateLimiter rateLimiter,
        IToolMediator toolMediator,
        ILogger<GeminiProvider> logger)
    {
        _client = new GenerativeClient(new GenerativeClientOptions
        {
            ApiKey = options.Value.ApiKey
        });
        _rateLimiter = rateLimiter;
        _toolMediator = toolMediator;
        _logger = logger;
    }

    public async Task<string> GenerateTextAsync(
        string prompt,
        Dictionary<string, object>? systemInstruction = null)
    {
        try
        {
            // Apply rate limiting
            if (!await _rateLimiter.AcquireAsync())
            {
                _logger.LogWarning("Rate limit exceeded, waiting...");
                await Task.Delay(1000);
            }

            var request = new GenerateContentRequest
            {
                Contents = {
                    new Content
                    {
                        Role = "user",
                        Parts = { new Part { Text = prompt } }
                    }
                }
            };

            if (systemInstruction != null)
            {
                request.Contents.Insert(0, new Content
                {
                    Role = "system",
                    Parts = { new Part { Text = systemInstruction["text"].ToString() } }
                });
            }

            var response = await _client.GenerateContentAsync(request);
            return response.Candidates[0].Content.Parts[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with Gemini");
            throw;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    // Implement other interface methods...
}
```

#### 3. Rate Limiter

Location: `/backend/LuzInga.Infrastructure/Common/`

```csharp
public interface IRateLimiter
{
    Task<bool> AcquireAsync();
    void Release();
}

public class TokenBucketRateLimiter : IRateLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly double _tokensPerSecond;
    private readonly int _burst;
    private double _tokens;
    private DateTime _lastRefill;

    public TokenBucketRateLimiter(double tokensPerSecond, int burst)
    {
        _semaphore = new SemaphoreSlim(1, 1);
        _tokensPerSecond = tokensPerSecond;
        _burst = burst;
        _tokens = burst;
        _lastRefill = DateTime.UtcNow;
    }

    public async Task<bool> AcquireAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            RefillTokens();
            if (_tokens >= 1)
            {
                _tokens -= 1;
                return true;
            }
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Release()
    {
        // Optional: Implement if needed for your use case
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timePassed = (now - _lastRefill).TotalSeconds;
        var newTokens = timePassed * _tokensPerSecond;
        _tokens = Math.Min(_burst, _tokens + newTokens);
        _lastRefill = now;
    }
}
```

## Memory & Storage

### Source Files

- `/legacy/src/core/supabase_rag.py`
- `/legacy/src/core/redis_cache.py`

### Target Implementation

#### 1. Supabase Integration

Location: `/backend/LuzInga.Infrastructure/Storage/`

```csharp
public class SupabaseOptions
{
    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string HuggingFaceApiKey { get; set; } = string.Empty;
    public string HuggingFaceModelName { get; set; } = "sentence-transformers/all-MiniLM-L6-v2";
}

public interface ISupabaseClient
{
    Task<T?> GetAsync<T>(string table, string id) where T : class;
    Task<IEnumerable<T>> GetAllAsync<T>(string table, string filter = "") where T : class;
    Task<T> InsertAsync<T>(string table, T data) where T : class;
    Task<T> UpdateAsync<T>(string table, string id, T data) where T : class;
    Task DeleteAsync(string table, string id);
    Task<IEnumerable<T>> MatchDocumentsAsync<T>(float[] embedding, int limit = 5, float threshold = 0.5f) where T : class;
}
```

#### 2. Document Models

Location: `/backend/LuzInga.Domain/Models/`

```csharp
public record Document
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public float[] Embedding { get; init; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record SearchResult
{
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public float Similarity { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

#### 3. RAG Service

Location: `/backend/LuzInga.Infrastructure/Storage/`

```csharp
public interface IRAGService
{
    Task<Document> AddDocumentAsync(string content, Dictionary<string, object>? metadata = null);
    Task<SearchResult> AddSearchResultAsync(string url, string content, string? summary = null);
    Task<IEnumerable<SearchResult>> GetSearchResultsAsync(string query, int limit = 5);
    Task<IEnumerable<Document>> SearchSimilarAsync(string query, int limit = 5);
    Task<string> GetContextAsync(string query);
}

public class SupabaseRAGService : IRAGService
{
    private readonly ISupabaseClient _supabase;
    private readonly IEmbeddingService _embeddings;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SupabaseRAGService> _logger;
    
    public SupabaseRAGService(
        ISupabaseClient supabase,
        IEmbeddingService embeddings,
        IDistributedCache cache,
        ILogger<SupabaseRAGService> logger)
    {
        _supabase = supabase;
        _embeddings = embeddings;
        _cache = cache;
        _logger = logger;
    }
    
    // Implementation details...
}
```

#### 4. Embedding Service

Location: `/backend/LuzInga.Infrastructure/AI/`

```csharp
public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
    Task<float[]> GetEmbeddingWithFallbackAsync(string text);
}

public class HuggingFaceEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly SentenceTransformer? _localModel;
    private readonly ILogger<HuggingFaceEmbeddingService> _logger;
    
    public HuggingFaceEmbeddingService(
        HttpClient httpClient,
        IDistributedCache cache,
        ILogger<HuggingFaceEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }
    
    // Implementation with API and local model fallback...
}
```

### Database Schema

```sql
-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Documents table
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    content TEXT NOT NULL,
    embedding vector(384), -- Dimension matches the model output
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create vector similarity search function
CREATE OR REPLACE FUNCTION match_documents (
    query_embedding vector(384),
    similarity_threshold float,
    match_count int
)
RETURNS TABLE (
    id UUID,
    content TEXT,
    metadata JSONB,
    similarity float
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        id,
        content,
        metadata,
        1 - (embedding <=> query_embedding) as similarity
    FROM documents
    WHERE 1 - (embedding <=> query_embedding) > similarity_threshold
    ORDER BY similarity DESC
    LIMIT match_count;
END;
$$;

-- Create index for faster similarity search
CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);
```

### Implementation Notes

1. Embedding Generation
    - Use HuggingFace API as primary source
    - Implement local model as fallback using ML.NET
    - Cache embeddings in Redis with TTL
    - Implement retry logic with exponential backoff

2. Vector Search
    - Use pgvector for similarity search
    - Implement caching for frequent queries
    - Add configurable similarity threshold
    - Optimize index for performance

3. Redis Caching
    - Cache embeddings to reduce API calls
    - Implement distributed locking
    - Set appropriate TTL for different types
    - Handle cache invalidation

4. Error Handling
    - Implement retry logic for API calls
    - Log detailed error information
    - Use circuit breaker pattern
    - Graceful degradation with fallbacks

## Tools & Utilities

### Source Files

- `/legacy/src/core/tools/`
- `/legacy/src/core/metrics_collector.py`

### Target Implementation

#### 1. Tool Infrastructure

Location: `/backend/LuzInga.Domain/Tools/`

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<object> ExecuteAsync(Dictionary<string, object> parameters);
}

public interface IToolMediator
{
    void RegisterTool(string name, ITool tool);
    Task<object> ExecuteToolAsync(string name, Dictionary<string, object> parameters);
}
```

#### 2. Tool Implementations

Location: `/backend/LuzInga.Infrastructure/Tools/`

```csharp
public class SearchTool : ITool
{
    private readonly ISearchService _searchService;
    
    public string Name => "search";
    public string Description => "Search for information";
    
    public async Task<object> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var query = parameters["query"].ToString();
        var numResults = parameters.GetValueOrDefault("num_results", 5);
        return await _searchService.SearchAsync(query, (int)numResults);
    }
}

public class FileOperationTool : ITool
{
    private readonly IFileService _fileService;
    private readonly ILogger<FileOperationTool> _logger;
    
    public async Task<object> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            var operation = parameters["operation"].ToString();
            var path = parameters["path"].ToString();

            // Security validation
            if (!IsPathSafe(path))
            {
                throw new SecurityException("Invalid path");
            }

            return operation switch
            {
                "read" => await _fileService.ReadFileAsync(path),
                "write" => await _fileService.WriteFileAsync(path, parameters["content"].ToString()),
                _ => throw new ArgumentException("Invalid operation")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing file operation");
            throw;
        }
    }

    private bool IsPathSafe(string path)
    {
        // Implement security checks
        return true;
    }
}
```

#### 3. Metrics

Location: `/backend/LuzInga.Infrastructure/Metrics/`

```csharp
public interface IMetricsCollector
{
    Task RecordInteractionAsync(
        string userId,
        string requestText,
        string responseText,
        DateTime startTime,
        bool cacheHit = false,
        int tokensUsed = 0,
        Dictionary<string, object>? context = null);
}

public class MetricsCollector : IMetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly ApplicationDbContext _dbContext;
    
    public async Task RecordInteractionAsync(
        string userId,
        string requestText,
        string responseText,
        DateTime startTime,
        bool cacheHit = false,
        int tokensUsed = 0,
        Dictionary<string, object>? context = null)
    {
        try
        {
            var interaction = new Interaction
            {
                UserId = userId,
                RequestText = requestText,
                ResponseText = responseText,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                CacheHit = cacheHit,
                TokensUsed = tokensUsed,
                Context = context
            };

            _dbContext.Interactions.Add(interaction);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording interaction");
            throw;
        }
    }
}
```

## Implementation Notes

### 1. Dependency Injection Setup

Location: `/backend/LuzInga.Application/DependencyInjection.cs`

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GeminiOptions>(
            configuration.GetSection("Gemini"));
            
        services.AddScoped<ILLMProvider, GeminiProvider>();
        services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();
        services.AddScoped<IToolMediator, ToolMediator>();
        
        services.AddStackExchangeRedisCache(options => {
            options.Configuration = configuration
                .GetConnectionString("Redis");
        });
        
        services.AddScoped<ICacheManager, RedisCacheManager>();
        services.AddScoped<IDistributedLockManager, RedisLockManager>();
        
        return services;
    }
}
```

### 2. Database Migrations

```bash
# Create initial migration
dotnet ef migrations add InitialCreate -p LuzInga.Infrastructure -s Api/RootBootstrapper

# Apply migrations
dotnet ef database update -p LuzInga.Infrastructure -s Api/RootBootstrapper
```

### 3. Configuration

Location: `/backend/Api/RootBootstrapper/appsettings.json`

```json
{
  "Gemini": {
    "ApiKey": "your-api-key",
    "ModelName": "gemini-1.5-flash",
    "TokensPerSecond": 0.25,
    "Burst": 5
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Horus;User Id=sa;Password=your-password;",
    "Redis": "localhost:6379"
  }
}
```

### 4. Required NuGet Packages

```xml
<ItemGroup>
    <PackageReference Include="Google.Cloud.AI.Generative.V1" Version="1.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="7.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="7.0.0" />
    <PackageReference Include="Polly" Version="7.2.3" />
    <PackageReference Include="Serilog" Version="2.12.0" />
    <PackageReference Include="AutoMapper" Version="12.0.0" />
</ItemGroup>
