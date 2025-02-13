namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<object> ExecuteAsync(Dictionary<string, object> parameters);
}