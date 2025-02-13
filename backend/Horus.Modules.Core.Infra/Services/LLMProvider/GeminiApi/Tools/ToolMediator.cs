using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;

public class ToolMediator : IToolMediator
{
    private readonly ILogger<ToolMediator> _logger;
    private readonly Dictionary<string, ITool> _tools = new();

    public ToolMediator(ILogger<ToolMediator> logger)
    {
        _logger = logger;
    }

    public void RegisterTool(string name, ITool tool)
    {
        _tools[name] = tool;
        _logger.LogInformation("Tool {Name} registered successfully", name);
    }

    public async Task<object> ExecuteToolAsync(string name, Dictionary<string, object> parameters)
    {
        if (!_tools.TryGetValue(name, out var tool))
        {
            _logger.LogError("Tool {Name} not found", name);
            throw new KeyNotFoundException($"Tool {name} not found");
        }

        _logger.LogInformation("Executing tool {Name} with parameters {@Parameters}", name, parameters);

        var result = await tool.ExecuteAsync(parameters);

        _logger.LogInformation("Tool {Name} executed successfully", name);

        return result;
    }
}