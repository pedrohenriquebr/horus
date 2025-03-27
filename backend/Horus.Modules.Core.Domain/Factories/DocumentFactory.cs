using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Domain.Factories;


public interface IDocumentFactory
{
    // Factory method
    public  Document CreatePending(
        Guid projectId,
        string sourceUri,
        string rawContent,
        string checksum,
        string chunkingStrategy);

    public  Document CreatePending(
        string sourceUri,
        string rawContent,
        string checksum,
        string chunkingStrategy);
    
    public  Document CreateMemory(string memory);
}

public class DocumentFactory : IDocumentFactory
{
    private readonly IDocumentChunkFactory _documentChunkFactory;

    public DocumentFactory(IDocumentChunkFactory documentChunkFactory)
    {
        _documentChunkFactory = documentChunkFactory;
    }
    
    
    // Factory method
    public  Document CreatePending(
        Guid projectId,
        string sourceUri,
        string rawContent,
        string checksum,
        string chunkingStrategy)
    {
        return new Document(
            id: Guid.NewGuid(),
            projectId: projectId,
            sourceUri : sourceUri,
            rawContent : rawContent,
            processedContent : string.Empty,
            checksum : checksum,
            chunkingStrategy : chunkingStrategy,
            statusId: (int)DocumentStatusEnum.Pending // Pending
        );
    }

    public  Document CreatePending(
        string sourceUri,
        string rawContent,
        string checksum,
        string chunkingStrategy)
    {
       return new Document(
            id: Guid.NewGuid(),
            projectId: null,
            sourceUri : sourceUri,
            rawContent : rawContent,
            processedContent : string.Empty,
            checksum : checksum,
            chunkingStrategy : chunkingStrategy,
            statusId: (int)DocumentStatusEnum.Pending // Pending
        );
    }

    public Document CreateMemory(string memory)
    {
        var newMemory = new Document(
            id: Guid.NewGuid(),
            projectId: null,
            sourceUri : "memory",
            rawContent : memory,
            processedContent : string.Empty,
            checksum : string.Empty,
            chunkingStrategy : "fixed_chunk",
            statusId: (int)DocumentStatusEnum.Completed // Pending
        );

        var chunk = _documentChunkFactory.CreatePending(
            newMemory.Id,
            1,
            memory,
            10,
            0,
            memory.Length - 1);
            
        newMemory.AddChunk(chunk);
        return newMemory;
    }
}


