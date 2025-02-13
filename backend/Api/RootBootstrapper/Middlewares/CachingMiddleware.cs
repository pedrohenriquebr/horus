using System.Net;
using System.Text;
using Horus.Modules.Core.Application.Extensions;
using Horus.Modules.Shared.Contracts.Configuration;
using Horus.Modules.Shared.Contracts.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Horus.RootBootstrapper.Middlewares;

//TODO: Remove this deprecated middleware
public class CachingMiddleware
{
    private readonly IDistributedCache _cache;
    private readonly IOptions<RedisConfig> _config;
    private readonly IRedisKeyFactory _keyFactory;
    private readonly RequestDelegate _next;

    public CachingMiddleware(RequestDelegate next, IDistributedCache cache, IOptions<RedisConfig> config,
        IRedisKeyFactory keyFactory)
    {
        _next = next;
        _cache = cache;
        _config = config;
        _keyFactory = keyFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        var key = _keyFactory.CreateCachingKey(path, context.Request.Query);

        // Try to get the cached result from the cache
        var cachedResult = await _cache.GetStringAsync(key);

        if (cachedResult != null)
        {
            // Return the cached result to the response
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResult);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using (var responseBodyStream = new MemoryStream())
        {
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);

                // Read the response body from the stream
                var responseContent = await ReadResponseBodyAsync(context.Response);

                if (context.Response.Headers.CacheControl.Any())
                    // Cache the response body
                    await _cache.SetRequestAsync(key, responseContent);

                // Write the response body to the original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);

                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            finally
            {
                // Reset the response body stream
                context.Response.Body = originalBodyStream;
            }
        }
    }


    public static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

        using var streamReader = new StreamReader(request.Body, Encoding.UTF8, false, -1, true);

        var requestBody = await streamReader.ReadToEndAsync();

        request.Body.Position = 0;

        return requestBody;
    }

    public static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);

        var responseBody = await new StreamReader(response.Body).ReadToEndAsync();

        response.Body.Seek(0, SeekOrigin.Begin);

        return responseBody;
    }
}

public sealed record CacheEntry(
    DateTime CreatedAt,
    string Payload
);