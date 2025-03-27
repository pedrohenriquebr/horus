namespace Horus.Modules.Core.Application.Usecases.GenerateText;

public class GenerateTextQueryResponse
{
    public string Text { get; init; }
    public List<SourceResponse>? Sources { get;  set; } = null;
}


public class SourceResponse
{
    public string Type { get; set; }
    public string Content { get; set; }
    public string? Url { get; set; }
    public string? DocumentName { get; set; }
    public DateTime CreatedAt { get; set; }
}