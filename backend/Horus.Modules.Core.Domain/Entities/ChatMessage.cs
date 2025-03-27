using Pgvector;

namespace Horus.Modules.Core.Domain.Entities;

public class ChatMessage : BaseEntityWithMetadata
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public Vector? QueryEmbedding { get; private set; }
    public Guid[] ReferenceChunkIds { get; private set; } = [];

    //ef core
    public ChatMessage(Guid id, Guid sessionId, string role, string content, Vector? queryEmbedding, Guid[] referenceChunkIds)
        {
            Id = id;
            SessionId = sessionId;
            Role = role;
            Content = content;
            QueryEmbedding = queryEmbedding;
            ReferenceChunkIds = referenceChunkIds;
        }
    
    // for crud generator
    public ChatMessage(Guid id, Guid sessionId, string role, string content, Vector? queryEmbedding,
        Guid[] referenceChunkIds, IReadOnlyDictionary<string, object> metadata)
    {
        Id = id;
        SessionId = sessionId;
        Role = role;
        Content = content;
        QueryEmbedding = queryEmbedding;
        ReferenceChunkIds = referenceChunkIds;
        this.Metadata = metadata;
    }
    
    protected ChatMessage()
    {
        
    }


    public void UpdateContent(string content)
    {
        Content = content;
    }

    public void UpdateMetadata(IReadOnlyDictionary<string, object> metadata)
    {
        Metadata = metadata;
    }

    public void UpdateQueryEmbedding(Vector? queryEmbedding)
    {
        QueryEmbedding = queryEmbedding;
    }

    public void UpdateReferenceChunkIds(Guid[] referenceChunkIds)
    {
        ReferenceChunkIds = referenceChunkIds;
    }

    public void UpdateRole(string role)
    {
        Role = role;
    }

    public void UpdateSessionId(Guid sessionId)
    {
        SessionId = sessionId;
    }
}