namespace Horus.Modules.Core.Domain.Entities;

public record SearchResult
{
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public float Similarity { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}