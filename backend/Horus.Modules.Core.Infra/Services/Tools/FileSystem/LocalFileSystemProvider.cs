using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Horus.Modules.Core.Infra.Services.Tools.FileSystem;

public class LocalFileSystemProvider : IFileSystemProvider
{
    private const int MaxRetries = 3;
    private readonly IContentTypeProvider _contentTypeProvider;
    private readonly ILogger<LocalFileSystemProvider> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public LocalFileSystemProvider(
        ILogger<LocalFileSystemProvider> logger,
        IContentTypeProvider contentTypeProvider)
    {
        _logger = logger;
        _contentTypeProvider = contentTypeProvider;

        _retryPolicy = Policy
            .Handle<IOException>()
            .Or<UnauthorizedAccessException>()
            .WaitAndRetryAsync(MaxRetries,
                retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Error during file operation (Attempt {RetryCount} of {MaxRetries}), retrying in {TimeSpan}ms",
                        retryCount,
                        MaxRetries,
                        timeSpan.TotalMilliseconds);
                });
    }

    public async Task<FileOperationResult> CreateDirectoryAsync(string path)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                Directory.CreateDirectory(path);
                await Task.CompletedTask;
            });

            var info = await GetInfoAsync(path);
            return new FileOperationResult { Success = true, File = info.File };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating directory: {Path}", path);
            return new FileOperationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<FileOperationResult> DeleteAsync(string path, bool recursive = false)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive);
                else if (File.Exists(path)) File.Delete(path);
                await Task.CompletedTask;
            });

            return new FileOperationResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting path: {Path}", path);
            return new FileOperationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<FileOperationResult> CopyAsync(string sourcePath, string destinationPath, bool overwrite = false)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                if (Directory.Exists(sourcePath))
                    CopyDirectory(sourcePath, destinationPath, overwrite);
                else
                    File.Copy(sourcePath, destinationPath, overwrite);
                await Task.CompletedTask;
            });

            var info = await GetInfoAsync(destinationPath);
            return new FileOperationResult { Success = true, File = info.File };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying from {Source} to {Destination}", sourcePath, destinationPath);
            return new FileOperationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<FileOperationResult> MoveAsync(string sourcePath, string destinationPath)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                if (Directory.Exists(sourcePath))
                    Directory.Move(sourcePath, destinationPath);
                else
                    File.Move(sourcePath, destinationPath);
                await Task.CompletedTask;
            });

            var info = await GetInfoAsync(destinationPath);
            return new FileOperationResult { Success = true, File = info.File };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving from {Source} to {Destination}", sourcePath, destinationPath);
            return new FileOperationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<FileOperationResult> GetInfoAsync(string path)
    {
        try
        {
            var info = await _retryPolicy.ExecuteAsync(async () =>
            {
                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    return new FileInfo
                    {
                        Path = path,
                        Name = dirInfo.Name,
                        Size = 0,
                        Extension = string.Empty,
                        IsDirectory = true,
                        ContentType = "application/x-directory",
                        LastModified = dirInfo.LastWriteTimeUtc
                    };
                }

                var fileInfo = new System.IO.FileInfo(path);
                _contentTypeProvider.TryGetContentType(path, out var contentType);
                return new FileInfo
                {
                    Path = path,
                    Name = fileInfo.Name,
                    Size = fileInfo.Length,
                    Extension = fileInfo.Extension,
                    IsDirectory = false,
                    ContentType = contentType ?? "application/octet-stream",
                    LastModified = fileInfo.LastWriteTimeUtc
                };
            });

            return new FileOperationResult { Success = true, File = info };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting info for path: {Path}", path);
            return new FileOperationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<IEnumerable<FileInfo>> ListDirectoryAsync(
        string path, string searchPattern = "*", bool recursive = false)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var directory = new DirectoryInfo(path);
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var items = directory.EnumerateFileSystemInfos(searchPattern, searchOption);

                return items.Select(item => new FileInfo
                {
                    Path = item.FullName,
                    Name = item.Name,
                    Size = item is System.IO.FileInfo file ? file.Length : 0,
                    Extension = item.Extension,
                    IsDirectory = item is DirectoryInfo,
                    ContentType = GetContentType(item.Name),
                    LastModified = item.LastWriteTimeUtc
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directory: {Path}", path);
            return Enumerable.Empty<FileInfo>();
        }
    }

    public async Task<string> ReadTextAsync(string path)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(() => File.ReadAllTextAsync(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading text from file: {Path}", path);
            throw;
        }
    }

    public async Task<Stream> ReadStreamAsync(string path)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(() =>
                Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading stream from file: {Path}", path);
            throw;
        }
    }

    public async Task<FileOperationResult> WriteTextAsync(string path, string content)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(() => File.WriteAllTextAsync(path, content));
            var info = await GetInfoAsync(path);
            return new FileOperationResult { Success = true, File = info.File };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing text to file: {Path}", path);
            return new FileOperationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<FileOperationResult> WriteStreamAsync(string path, Stream content)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using var fileStream = File.Create(path);
                await content.CopyToAsync(fileStream);
            });

            var info = await GetInfoAsync(path);
            return new FileOperationResult { Success = true, File = info.File };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing stream to file: {Path}", path);
            return new FileOperationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<bool> ExistsAsync(string path)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(() =>
                Task.FromResult(File.Exists(path) || Directory.Exists(path)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of path: {Path}", path);
            return false;
        }
    }

    private void CopyDirectory(string sourcePath, string destinationPath, bool overwrite)
    {
        if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);

        foreach (var file in Directory.GetFiles(sourcePath))
        {
            var dest = Path.Combine(destinationPath, Path.GetFileName(file));
            File.Copy(file, dest, overwrite);
        }

        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            var dest = Path.Combine(destinationPath, Path.GetFileName(dir));
            CopyDirectory(dir, dest, overwrite);
        }
    }

    private string GetContentType(string fileName)
    {
        _contentTypeProvider.TryGetContentType(fileName, out var contentType);
        return contentType ?? "application/octet-stream";
    }
}