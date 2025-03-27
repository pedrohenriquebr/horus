using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Domain.Events;

public class DocumentDto
{
    public Guid Id { get;  set; }
    public Guid? ProjectId { get;  set; }
    public string SourceUri { get;  set; }
    public string RawContent { get;  set; }
    public string ProcessedContent { get;  set; }
    public string Checksum { get;  set; }
    public int StatusId { get;  set; } = 1;
    public string? ErrorCode { get;  set; }
    public string? ErrorMessage { get;  set; }
    public int RetryCount { get;  set; }
    public DateTime? LastProcessedAt { get;  set; }
    public string ChunkingStrategy { get;  set; }

    public static DocumentDto MapFrom(Document document)
    {
        return new DocumentDto()
        {
            Id = document.Id,
            ProjectId = document.ProjectId,
            SourceUri = document.SourceUri,
            RawContent = document.RawContent,
            ProcessedContent = document.ProcessedContent,
            Checksum = document.Checksum,
            StatusId = document.StatusId,
            ErrorCode = document.ErrorCode,
            ErrorMessage = document.ErrorMessage,
            RetryCount = document.RetryCount,
            LastProcessedAt = document.LastProcessedAt,
            ChunkingStrategy = document.ChunkingStrategy
        };
    }
}