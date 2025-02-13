namespace Horus.Modules.Core.Application.Services;

public record MemoryItem(
    string Content,
    DateTime CreatedAt,
    string Source,
    Dictionary<string, object> Metadata
);