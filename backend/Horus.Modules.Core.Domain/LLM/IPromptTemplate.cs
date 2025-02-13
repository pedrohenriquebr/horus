namespace Horus.Modules.Core.Domain.LLM;

public interface IPromptTemplate
{
    string Name { get; }
    string Format(Dictionary<string, string> variables);
    Dictionary<string, string> GetRequiredVariables();
}