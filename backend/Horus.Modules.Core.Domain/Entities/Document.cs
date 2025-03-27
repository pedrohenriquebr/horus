using System;
using System.Collections.Generic;
using Horus.Modules.Core.Domain.Events;

namespace Horus.Modules.Core.Domain.Entities;

// Aggregate Root
public class Document : BaseEntityWithMetadata
{
    public Guid Id { get; private init; }
    public Guid? ProjectId { get; private init; }
    public string SourceUri { get; private init; }
    public string RawContent { get; private set; }
    public string ProcessedContent { get; private set; }
    public string Checksum { get; private set; }
    public int StatusId { get; private set; } = 1;
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? LastProcessedAt { get; private set; }
    public string ChunkingStrategy { get; private init; }
    
    public virtual ICollection<DocumentChunk> Chunks { get; } = new List<DocumentChunk>();
    
    
    // for ef core
    public Document(Guid id, Guid? projectId, string sourceUri, string rawContent, string processedContent,
        string checksum, int statusId, string? errorCode, string? errorMessage, int retryCount,
        DateTime? lastProcessedAt, string chunkingStrategy)
    {
        Id = id;
        ProjectId = projectId;
        SourceUri = sourceUri;
        RawContent = rawContent;
        ProcessedContent = processedContent;
        Checksum = checksum;
        StatusId = statusId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RetryCount = retryCount;
        LastProcessedAt = lastProcessedAt;
        ChunkingStrategy = chunkingStrategy;
    }
    
    
    //for factory
    public Document(Guid id, Guid? projectId, string sourceUri, string rawContent, string processedContent , string checksum,
        string chunkingStrategy, int statusId)
    {
        Id = id;
        ProjectId = projectId;
        SourceUri = sourceUri;
        RawContent = rawContent;
        Checksum = checksum;
        ChunkingStrategy = chunkingStrategy;
        StatusId = statusId;
        ProcessedContent = processedContent;
        
        AddDomainEvent(new DocumentAddedEvent(this));
    }


    protected Document()
    {
    }

    // Domain methods
    public void StartProcessing()
    {
        StatusId = 2; // Processing
        LastProcessedAt = DateTime.UtcNow;
        AddDomainEvent(new DocumentStartedChunking(this));
    }

    public void FailProcessing(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        StatusId = 4; // Failed
        RetryCount++;
    }

    public void ProcessContent(string processedContent)
    {
        ProcessedContent = processedContent;
        LastProcessedAt = DateTime.UtcNow;
    }

    public void CompleteProcessing()
    {
        StatusId = 3; // Completed
        LastProcessedAt = DateTime.UtcNow;
    }
    
    
    public void AddChunk(DocumentChunk chunk)
    {
        // Business rules validation
        if (chunk.DocumentId != Id)
            throw new InvalidOperationException("Chunk does not belong to this document");

        Chunks.Add(chunk);
    }

    public void FinishChunking()
    {
        StatusId = (int)DocumentStatusEnum.Completed;
        LastProcessedAt = DateTime.UtcNow;
        MarkAsUpdated();
        AddDomainEvent(new DocumentFinishedChunkingEvent(this));
    }
}