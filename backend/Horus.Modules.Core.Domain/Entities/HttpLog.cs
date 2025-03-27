namespace Horus.Modules.Core.Domain.Entities;

public class HttpLog : BaseEntityWithMetadata
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, string> RequestHeaders { get; set; } = new();
    public string? RequestBody { get; set; }
    public Dictionary<string, string>? ResponseHeaders { get; set; }
    public string? ResponseBody { get; set; }
    public short? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int? DurationMs { get; set; }
    public string Service { get; set; } = string.Empty;
}