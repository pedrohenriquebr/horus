using Horus.Modules.Core.Application.Abstractions.Messaging;

namespace Horus.Modules.Core.Application.Usecases.GenerateText;

public class GenerateTextQuery : IQuery<GenerateTextQueryResponse>
{
    public string Prompt { get; init; }
    public Dictionary<string, string> UserInfo { get; set; }
    public string? ImagePath { get; set; }
    public string? AudioPath { get; set; }
    public string SystemInstructions { get; set; }
}