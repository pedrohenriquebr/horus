# Technical Specification - Phase 3 Migration

## Overview

This document details the technical specifications for migrating the audio processing, vision processing, and dashboard
modules from the legacy Python codebase to the new .NET Core solution.

## Table of Contents

1. [Audio Processing](#audio-processing)
2. [Vision Processing](#vision-processing)
3. [Dashboard](#dashboard)
4. [Implementation Notes](#implementation-notes)

## Audio Processing

### Source Files

- `/legacy/src/audio/speech_handler.py`

### Target Implementation

#### 1. Core Interfaces

Location: `/backend/LuzInga.Domain/Audio/`

```csharp
public interface IAudioProcessor
{
    Task<string> TranscribeAudioAsync(string audioPath);
    Task<AudioAnalysisResult> AnalyzeAudioAsync(string audioPath);
    Task<byte[]> ProcessAudioAsync(string audioPath, AudioProcessingOptions options);
}

public interface IAudioCache
{
    Task<AudioCacheItem?> GetAsync(string audioId);
    Task SetAsync(string audioId, AudioCacheItem item, TimeSpan? expiry = null);
}

public record AudioProcessingOptions
{
    public int SampleRate { get; init; } = 16000;
    public string OutputFormat { get; init; } = "wav";
    public bool RemoveNoise { get; init; } = false;
    public bool NormalizeVolume { get; init; } = true;
}

public record AudioAnalysisResult
{
    public double Duration { get; init; }
    public double AverageAmplitude { get; init; }
    public Dictionary<string, double> Metrics { get; init; } = new();
}

public record AudioCacheItem
{
    public string Transcription { get; init; } = string.Empty;
    public AudioAnalysisResult? Analysis { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

#### 2. Audio Processing Implementation

Location: `/backend/LuzInga.Infrastructure/Audio/`

```csharp
public class AudioProcessor : IAudioProcessor
{
    private readonly ILogger<AudioProcessor> _logger;
    private readonly IAudioCache _cache;
    private readonly SpeechConfig _speechConfig;

    public AudioProcessor(
        ILogger<AudioProcessor> logger,
        IAudioCache cache,
        IOptions<SpeechConfig> speechConfig)
    {
        _logger = logger;
        _cache = cache;
        _speechConfig = speechConfig.Value;
    }

    public async Task<string> TranscribeAudioAsync(string audioPath)
    {
        try
        {
            using var audioInput = AudioConfig.FromWavFileInput(audioPath);
            using var recognizer = new SpeechRecognizer(_speechConfig, audioInput);

            var result = await recognizer.RecognizeOnceAsync();
            return result.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transcribing audio file: {Path}", audioPath);
            throw;
        }
    }

    public async Task<AudioAnalysisResult> AnalyzeAudioAsync(string audioPath)
    {
        try
        {
            using var audioFile = new AudioFileReader(audioPath);
            var duration = audioFile.TotalTime.TotalSeconds;
            var samples = new float[audioFile.Length];
            audioFile.Read(samples, 0, samples.Length);

            var amplitude = samples.Select(Math.Abs).Average();

            return new AudioAnalysisResult
            {
                Duration = duration,
                AverageAmplitude = amplitude,
                Metrics = CalculateAudioMetrics(samples)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing audio file: {Path}", audioPath);
            throw;
        }
    }

    private Dictionary<string, double> CalculateAudioMetrics(float[] samples)
    {
        // Implementation
        return new Dictionary<string, double>();
    }
}
```

#### 3. Audio Cache Implementation

Location: `/backend/LuzInga.Infrastructure/Audio/`

```csharp
public class AudioCache : IAudioCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<AudioCache> _logger;

    public AudioCache(
        IDistributedCache cache,
        ILogger<AudioCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<AudioCacheItem?> GetAsync(string audioId)
    {
        try
        {
            var data = await _cache.GetAsync(GetCacheKey(audioId));
            if (data == null)
                return null;

            return JsonSerializer.Deserialize<AudioCacheItem>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audio cache item: {Id}", audioId);
            return null;
        }
    }

    public async Task SetAsync(string audioId, AudioCacheItem item, TimeSpan? expiry = null)
    {
        try
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(item);
            await _cache.SetAsync(
                GetCacheKey(audioId),
                data,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromDays(1)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting audio cache item: {Id}", audioId);
            throw;
        }
    }

    private static string GetCacheKey(string audioId) => $"audio:{audioId}";
}
```

## Vision Processing

### Source Files

- `/legacy/src/vision/image_processor.py`

### Target Implementation

#### 1. Core Interfaces

Location: `/backend/LuzInga.Domain/Vision/`

```csharp
public interface IImageProcessor
{
    Task<ImageAnalysisResult> AnalyzeImageAsync(string imagePath);
    Task<byte[]> ProcessImageAsync(string imagePath, ImageProcessingOptions options);
    Task<string> ExtractTextAsync(string imagePath);
}

public interface IImageCache
{
    Task<ImageCacheItem?> GetAsync(string imageId);
    Task SetAsync(string imageId, ImageCacheItem item, TimeSpan? expiry = null);
}

public record ImageProcessingOptions
{
    public int MaxWidth { get; init; } = 1920;
    public int MaxHeight { get; init; } = 1080;
    public string OutputFormat { get; init; } = "jpg";
    public int Quality { get; init; } = 85;
    public bool PreserveMetadata { get; init; } = true;
}

public record ImageAnalysisResult
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Format { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public Dictionary<string, double> Metrics { get; init; } = new();
}

public record ImageCacheItem
{
    public ImageAnalysisResult? Analysis { get; init; }
    public string ExtractedText { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; }
}
```

#### 2. Image Processing Implementation

Location: `/backend/LuzInga.Infrastructure/Vision/`

```csharp
public class ImageProcessor : IImageProcessor
{
    private readonly ILogger<ImageProcessor> _logger;
    private readonly IImageCache _cache;
    private readonly ComputerVisionClient _visionClient;

    public ImageProcessor(
        ILogger<ImageProcessor> logger,
        IImageCache cache,
        IOptions<VisionConfig> visionConfig)
    {
        _logger = logger;
        _cache = cache;
        _visionClient = new ComputerVisionClient(
            new ApiKeyServiceClientCredentials(visionConfig.Value.ApiKey))
        {
            Endpoint = visionConfig.Value.Endpoint
        };
    }

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(string imagePath)
    {
        try
        {
            using var image = await Image.LoadAsync(imagePath);
            var metrics = await AnalyzeImageWithAzureAsync(imagePath);

            return new ImageAnalysisResult
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name ?? "unknown",
                SizeBytes = new FileInfo(imagePath).Length,
                Metrics = metrics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing image: {Path}", imagePath);
            throw;
        }
    }

    private async Task<Dictionary<string, double>> AnalyzeImageWithAzureAsync(string imagePath)
    {
        using var imageStream = File.OpenRead(imagePath);
        var features = new List<VisualFeatureTypes?>
        {
            VisualFeatureTypes.Categories,
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Tags
        };

        var analysis = await _visionClient.AnalyzeImageInStreamAsync(
            imageStream, features);

        return new Dictionary<string, double>
        {
            ["confidence"] = analysis.Description.Captions[0].Confidence,
            // Add other metrics
        };
    }

    public async Task<byte[]> ProcessImageAsync(
        string imagePath,
        ImageProcessingOptions options)
    {
        try
        {
            using var image = await Image.LoadAsync(imagePath);
            
            // Resize if needed
            if (image.Width > options.MaxWidth || image.Height > options.MaxHeight)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(options.MaxWidth, options.MaxHeight)
                }));
            }

            // Save with specified format and quality
            using var ms = new MemoryStream();
            await image.SaveAsync(ms, new JpegEncoder
            {
                Quality = options.Quality
            });

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image: {Path}", imagePath);
            throw;
        }
    }

    public async Task<string> ExtractTextAsync(string imagePath)
    {
        try
        {
            using var imageStream = File.OpenRead(imagePath);
            var textHeaders = await _visionClient.ReadInStreamAsync(imageStream);
            var operationLocation = textHeaders.OperationLocation;

            // Get Operation ID from the URL
            var operationId = operationLocation.Split('/').Last();

            // Wait for the OCR operation to complete
            ReadOperationResult results;
            do
            {
                results = await _visionClient.GetReadResultAsync(Guid.Parse(operationId));
                await Task.Delay(100);
            }
            while (results.Status == OperationStatusCodes.Running ||
                   results.Status == OperationStatusCodes.NotStarted);

            var text = new StringBuilder();
            if (results.Status == OperationStatusCodes.Succeeded)
            {
                foreach (var page in results.AnalyzeResult.ReadResults)
                {
                    foreach (var line in page.Lines)
                    {
                        text.AppendLine(line.Text);
                    }
                }
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from image: {Path}", imagePath);
            throw;
        }
    }
}
```

## Dashboard

### Source Files

- `/legacy/src/dashboard/`

### Target Implementation

#### 1. Core Interfaces

Location: `/backend/LuzInga.Domain/Dashboard/`

```csharp
public interface IDashboardService
{
    Task<DashboardMetrics> GetMetricsAsync(DateTime start, DateTime end);
    Task<IEnumerable<ChartData>> GetChartDataAsync(string metric, DateTime start, DateTime end);
    Task<IEnumerable<AlertData>> GetActiveAlertsAsync();
}

public record DashboardMetrics
{
    public int TotalRequests { get; init; }
    public int SuccessfulRequests { get; init; }
    public int FailedRequests { get; init; }
    public double AverageResponseTime { get; init; }
    public Dictionary<string, double> CustomMetrics { get; init; } = new();
}

public record ChartData
{
    public DateTime Timestamp { get; init; }
    public string Label { get; init; } = string.Empty;
    public double Value { get; init; }
}

public record AlertData
{
    public string Id { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
```

#### 2. Dashboard Implementation

Location: `/backend/LuzInga.Infrastructure/Dashboard/`

```csharp
public class DashboardService : IDashboardService
{
    private readonly ILogger<DashboardService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMetricsCollector _metricsCollector;

    public DashboardService(
        ILogger<DashboardService> logger,
        ApplicationDbContext dbContext,
        IMetricsCollector metricsCollector)
    {
        _logger = logger;
        _dbContext = dbContext;
        _metricsCollector = metricsCollector;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(DateTime start, DateTime end)
    {
        try
        {
            var metrics = await _dbContext.Interactions
                .Where(i => i.StartTime >= start && i.StartTime <= end)
                .GroupBy(_ => 1)
                .Select(g => new DashboardMetrics
                {
                    TotalRequests = g.Count(),
                    SuccessfulRequests = g.Count(i => i.IsSuccess),
                    FailedRequests = g.Count(i => !i.IsSuccess),
                    AverageResponseTime = g.Average(i => 
                        (i.EndTime - i.StartTime).TotalMilliseconds)
                })
                .FirstOrDefaultAsync() ?? new DashboardMetrics();

            // Add custom metrics
            metrics = metrics with
            {
                CustomMetrics = await GetCustomMetricsAsync(start, end)
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard metrics");
            throw;
        }
    }

    private async Task<Dictionary<string, double>> GetCustomMetricsAsync(
        DateTime start,
        DateTime end)
    {
        // Implementation
        return new Dictionary<string, double>();
    }

    public async Task<IEnumerable<ChartData>> GetChartDataAsync(
        string metric,
        DateTime start,
        DateTime end)
    {
        try
        {
            var data = await _dbContext.Interactions
                .Where(i => i.StartTime >= start && i.StartTime <= end)
                .GroupBy(i => i.StartTime.Date)
                .Select(g => new ChartData
                {
                    Timestamp = g.Key,
                    Label = g.Key.ToString("yyyy-MM-dd"),
                    Value = metric switch
                    {
                        "requests" => g.Count(),
                        "success_rate" => (double)g.Count(i => i.IsSuccess) / g.Count(),
                        "response_time" => g.Average(i => 
                            (i.EndTime - i.StartTime).TotalMilliseconds),
                        _ => 0
                    }
                })
                .ToListAsync();

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart data for metric: {Metric}", metric);
            throw;
        }
    }

    public async Task<IEnumerable<AlertData>> GetActiveAlertsAsync()
    {
        try
        {
            return await _dbContext.Alerts
                .Where(a => !a.Resolved)
                .Select(a => new AlertData
                {
                    Id = a.Id.ToString(),
                    Severity = a.Severity,
                    Message = a.Message,
                    Timestamp = a.CreatedAt
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            throw;
        }
    }
}
```

## Implementation Notes

### 1. Required NuGet Packages

```xml
<ItemGroup>
    <!-- Audio Processing -->
    <PackageReference Include="Microsoft.CognitiveServices.Speech" Version="1.31.0" />
    <PackageReference Include="NAudio" Version="2.2.1" />
    
    <!-- Image Processing -->
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.1" />
    <PackageReference Include="Microsoft.Azure.CognitiveServices.Vision.ComputerVision" Version="7.0.1" />
    
    <!-- Dashboard -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="7.0.0" />
</ItemGroup>
```

### 2. Configuration

Location: `/backend/Api/RootBootstrapper/appsettings.json`

```json
{
  "Speech": {
    "SubscriptionKey": "your-key",
    "Region": "your-region"
  },
  "Vision": {
    "ApiKey": "your-key",
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/"
  },
  "Dashboard": {
    "RefreshInterval": 60,
    "RetentionDays": 30
  }
}
```

### 3. Database Migrations

```bash
# Create migrations for new entities
dotnet ef migrations add AddDashboardTables -p LuzInga.Infrastructure -s Api/RootBootstrapper

# Apply migrations
dotnet ef database update -p LuzInga.Infrastructure -s Api/RootBootstrapper
```
