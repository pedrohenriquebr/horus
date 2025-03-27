using System.Text.Json;
using Horus.Modules.Core.Domain.Events;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Pgvector;

namespace Horus.Modules.Core.Domain.Entities;

public class DocumentChunk : BaseEntityWithMetadata
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int ChunkNumber { get; private set; }
    public string Content { get; private set; }
    public int TokenCount { get; private set; }
    public int StartOffset { get; private set; }
    public int EndOffset { get; private set; }
    public int? StatusId { get; private set; }
    public string? ProcessingError { get; private set; }
    public string? EmbeddingError { get; private set; }
    public NpgsqlTsVector SearchVector { get; private set; }
    public string LanguageCode { get; private set; }
    public virtual ChunkEmbedding? Embedding { get; private set; }

    
    protected DocumentChunk() { }
    
    public DocumentChunk(Guid id, Guid documentId, int chunkNumber, string content, int tokenCount, int startOffset, int endOffset, int? statusId, string? processingError, string? embeddingError, NpgsqlTsVector searchVector, string languageCode, ChunkEmbedding? embedding)
    {
        Id = id;
        DocumentId = documentId;
        ChunkNumber = chunkNumber;
        Content = content;
        TokenCount = tokenCount;
        StartOffset = startOffset;
        EndOffset = endOffset;
        StatusId = statusId;
        ProcessingError = processingError;
        EmbeddingError = embeddingError;
        SearchVector = searchVector;
        LanguageCode = languageCode;
        Embedding = embedding;
    }

    

    public DocumentChunk(Guid id, Guid documentId, int chunkNumber, string content, int tokenCount, int startOffset, int endOffset, int statusId)
    {
        Id = id;
        DocumentId = documentId;
        ChunkNumber = chunkNumber;
        Content = content;
        TokenCount = tokenCount;
        StartOffset = startOffset;
        EndOffset = endOffset;
        StatusId = statusId;
        LanguageCode = "eng";
        
        AddDomainEvent(new DocumentChunkCreatedEvent(this));
    }
    
    public DocumentChunk(Guid id, int chunkNumber, string content, int tokenCount, int startOffset, int endOffset, int statusId)
    {
        Id = id;
        DocumentId = Guid.Empty;
        ChunkNumber = chunkNumber;
        Content = content;
        TokenCount = tokenCount;
        StartOffset = startOffset;
        EndOffset = endOffset;
        StatusId = statusId;
        LanguageCode = "eng";
        
        AddDomainEvent(new DocumentChunkCreatedEvent(this));
    }


    public void UpdateContent(string newContent, int newTokenCount)
    {
        Content = newContent;
        TokenCount = newTokenCount;
        MarkAsUpdated();
    }

    public void StartEmbedding()
    {
        this.StatusId = (int?)DocumentStatusEnum.Processing;
        AddDomainEvent(new DocumentChunkEmbeddingStartedEvent(this));
    }

    public void FailProcessing(string exMessage)
    {
        this.StatusId = (int?)DocumentStatusEnum.Failed;
        this.ProcessingError = exMessage;
    }

    public void FinishEmbedding(float[] embedding, int modelId)
    {
        this.EmbeddingError = null;
        this.ProcessingError = null;
        
        if(this.Embedding is null)
            this.Embedding = new ChunkEmbedding(this.Id, modelId, new Vector(JsonSerializer.Serialize(embedding)));
        else
        {
            this.Embedding?.UpdateModelId(modelId);
            this.Embedding?.UpdateEmbedding(new Vector(JsonSerializer.Serialize(embedding)));
        }
        this.StatusId = (int?)DocumentStatusEnum.Completed;
        // AddDomainEvent(new DocumentChunkEmbeddingUpdatedEvent(this));
    }

    public void FailEmbedding(string exMessage)
    {
        this.EmbeddingError = exMessage;
        this.StatusId = (int?)DocumentStatusEnum.Failed;
        // AddDomainEvent(new DocumentChunkEmbeddingFailedEvent(this));
    }
}