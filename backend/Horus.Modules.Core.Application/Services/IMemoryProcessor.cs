namespace Horus.Modules.Core.Application.Services;

public interface IMemoryProcessor
{
    Task<string> ProcessMemoryTags(string response, Dictionary<string, string> userInfo);
}