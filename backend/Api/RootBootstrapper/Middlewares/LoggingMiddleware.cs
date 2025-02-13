using System.Text;
using Horus.Modules.Shared.Contracts.Events;
using Horus.Modules.Shared.Contracts.Services;

namespace Horus.RootBootstrapper.Middlewares;

public class LoggingMiddleware
{
    private readonly IAuditLogger _logger;
    private readonly RequestDelegate _next;

    public LoggingMiddleware(RequestDelegate next, IAuditLogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        await _logger.LogRecent(new ApplicationAccessedEvent(
            DateTime.UtcNow,
            context.Request.Path.Value,
            context.User?.Identity?.Name ?? "anonymous",
            context.Request.Method,
            context.Connection.RemoteIpAddress?.ToString()
        ));
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