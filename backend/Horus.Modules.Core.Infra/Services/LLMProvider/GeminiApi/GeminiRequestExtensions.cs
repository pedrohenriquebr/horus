using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi;

public static class GeminiRequestExtensions
{
    public static GenerateContentRequest AddTool<T>(this GenerateContentRequest request, string name,
        string description)
        where T : IToolParameter
    {
        var properties = new Dictionary<string, PropertyDefinition>();
        var type = typeof(T);
        var required = new List<string>();

        foreach (var prop in type.GetProperties())
        {
            var paramType = GetParameterType(prop.PropertyType);
            var propDescription = prop.GetCustomAttribute<DescriptionAttribute>()?.Description
                                  ?? $"Parameter {prop.Name}";

            properties.Add(prop.Name.ToLower(), new PropertyDefinition(
                paramType,
                propDescription
            ));

            if (!prop.PropertyType.IsGenericType ||
                prop.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>))
                required.Add(prop.Name.ToLower());
        }

        var functionDeclaration = new FunctionDeclaration(
            name,
            description,
            new ParameterDefinition(
                "object",
                properties,
                required
            )
        );

        var tools = request.Tools ?? new List<Tool>();
        tools.Add(new Tool(new List<FunctionDeclaration> { functionDeclaration }));

        return request with { Tools = tools };
    }

    public static GenerateContentRequest AddWebSearchTool(this GenerateContentRequest request)
    {
        return request.AddTool<SearchParameters>("search", "Perform a web search to find relevant information. " +
                                                           "I will analyze the search results and provide you with a comprehensive summary that includes: " +
                                                           "1) Key findings and main points, " +
                                                           "2) Important details and context, " +
                                                           "3) Credible sources with direct links to verify the information. " +
                                                           "The summary will be clear, accurate and well-organized.");
    }

    private static string GetParameterType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(int?)) return "integer";
        if (type == typeof(double) || type == typeof(double?)) return "number";
        if (type == typeof(bool) || type == typeof(bool?)) return "boolean";
        if (type.IsEnum) return "string";
        return "object";
    }

    public static bool HasFunctionCall(this GenerateContentResponse response)
    {
        return response.Candidates.Any(c => c.Content.Parts.Any(d => d.FunctionCall != null));
    }

    public static async Task<string> HandleFunctionCallsAsync(
        this GenerateContentResponse response,
        IToolMediator toolMediator)
    {
        var result = new StringBuilder();

        foreach (var candidate in response.Candidates)
        foreach (var part in candidate.Content.Parts)
            if (part.FunctionCall != null)
            {
                var functionCall = part.FunctionCall;
                var parameters = functionCall.Args.Deserialize<Dictionary<string, object>>();

                var toolResult = await toolMediator.ExecuteToolAsync(
                    functionCall.Name,
                    parameters
                );

                result.AppendLine(toolResult.ToString());
            }
            else
            {
                result.AppendLine(part.Text);
            }

        return result.ToString().Trim();
    }
}