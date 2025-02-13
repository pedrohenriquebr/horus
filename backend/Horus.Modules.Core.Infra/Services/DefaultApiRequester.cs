using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Infra.Services;

public class ApiLoggingHandler : DelegatingHandler
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ApiLoggingHandler> _logger;

    public ApiLoggingHandler(ILogger<ApiLoggingHandler> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await LogRequest(request, cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);
        await LogResponse(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task LogRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Request: {request.Method} {request.RequestUri}");

            if (request.Content != null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken);
                sb.AppendLine("Content:");
                sb.AppendLine(JsonSerializer.Serialize(JsonDocument.Parse(content).RootElement, _jsonOptions));
            }

            _logger.LogInformation(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging request");
        }
    }

    private async Task LogResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Response: {response.StatusCode} {response.RequestMessage?.RequestUri}");

            if (response.Content != null)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                sb.AppendLine("Content:");

                if (IsJsonContent(response.Content))
                    sb.AppendLine(JsonSerializer.Serialize(JsonDocument.Parse(content).RootElement, _jsonOptions));
                else
                    sb.AppendLine(content);
            }

            _logger.LogInformation(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging response");
        }
    }

    private static bool IsJsonContent(HttpContent content)
    {
        return (content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) ??
                false) ||
               (content.Headers.ContentType?.MediaType?.Contains("text", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}