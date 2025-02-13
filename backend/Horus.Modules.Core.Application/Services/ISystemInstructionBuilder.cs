namespace Horus.Modules.Core.Application.Services;

public interface ISystemInstructionBuilder
{
    string Build(Dictionary<string, string> userInfo);
}