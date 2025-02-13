namespace Horus.Modules.Core.Infra.Services.Tools.FileSystem;

public record FileInfo
{
    public string Path { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Extension { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTimeOffset LastModified { get; init; }
}