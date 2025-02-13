namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

public class SearchWebTool : ITool
{
    public string Name => "search";
    public string Description => "Searches for information and return all results summarized with sources";

    public async Task<object> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.Delay(1000);
        return "";
    }
}