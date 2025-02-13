using System.ComponentModel;

namespace Horus.Modules.Core.Infra.Services.LLMProvider.GeminiApi.Tools;

public class SearchParameters : IToolParameter
{
    [Description("Query to search for. Eg: 'What is the capital of France?'")]
    public string Query { get; set; }

    [Description("Number of results to return, by default is 5")]
    public int? NumResults { get; set; } = 5;
}