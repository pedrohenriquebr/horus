namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;

public interface IToolMediator
{
    void RegisterTool(string name, ITool tool);
    Task<object> ExecuteToolAsync(string name, Dictionary<string, object> parameters);
}