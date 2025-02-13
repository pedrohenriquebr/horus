namespace Horus.Modules.Core.Infra.Services.Tools.FileSystem;

public interface IFileSystemProvider
{
    Task<FileOperationResult> CreateDirectoryAsync(string path);
    Task<FileOperationResult> DeleteAsync(string path, bool recursive = false);
    Task<FileOperationResult> CopyAsync(string sourcePath, string destinationPath, bool overwrite = false);
    Task<FileOperationResult> MoveAsync(string sourcePath, string destinationPath);
    Task<FileOperationResult> GetInfoAsync(string path);
    Task<IEnumerable<FileInfo>> ListDirectoryAsync(string path, string searchPattern = "*", bool recursive = false);
    Task<string> ReadTextAsync(string path);
    Task<Stream> ReadStreamAsync(string path);
    Task<FileOperationResult> WriteTextAsync(string path, string content);
    Task<FileOperationResult> WriteStreamAsync(string path, Stream content);
    Task<bool> ExistsAsync(string path);
}