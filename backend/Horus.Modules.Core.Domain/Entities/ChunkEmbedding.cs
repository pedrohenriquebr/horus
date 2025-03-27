using Pgvector;

namespace Horus.Modules.Core.Domain.Entities;

public class ChunkEmbedding : BaseEntity
{
    public Guid ChunkId { get; private set; }
    public int ModelId { get; private set; }
    public Vector Embedding { get; private set; } = new Vector("[0]");

    public virtual EmbeddingModel EmbeddingModel { get; set; } = null!;
    
    
    // for ef core
    protected ChunkEmbedding()
    {
        
    }

    public ChunkEmbedding(Guid chunkId, int modelId, Vector embedding)
    {
        ChunkId = chunkId;
        ModelId = modelId;
        Embedding = embedding;
    }

    public void UpdateModelId(int modelId)
    {
        this.ModelId = modelId;
    }

    public void UpdateEmbedding(Vector vector)
    {
        this.Embedding = vector;
    }
}