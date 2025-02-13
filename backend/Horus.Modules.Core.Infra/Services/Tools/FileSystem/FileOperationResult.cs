namespace Horus.Modules.Core.Infra.Services.Tools.FileSystem;

public record FileOperationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public FileInfo? File { get; init; }
}