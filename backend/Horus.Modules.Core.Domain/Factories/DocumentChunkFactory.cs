using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Domain.Factories;
public interface  IDocumentChunkFactory
{
    DocumentChunk CreatePending(
        Guid documentId,
        int chunkNumber,
        string content,
        int tokenCount,
        int startOffset,
        int endOffset);

    public DocumentChunk CreatePending(
        int chunkNumber,
        string content,
        int tokenCount,
        int startOffset,
        int endOffset);
}
public class DocumentChunkFactory : IDocumentChunkFactory
{
    public  DocumentChunk CreatePending(
        Guid documentId,
        int chunkNumber,
        string content,
        int tokenCount,
        int startOffset,
        int endOffset)
    {
        return new DocumentChunk(
            id: Guid.NewGuid(),
            documentId: documentId,
            chunkNumber: chunkNumber,
            content: content,
            tokenCount: tokenCount,
            startOffset: startOffset,
            endOffset: endOffset,
            statusId: 1 // Pending
        );
    }
    
    public  DocumentChunk CreatePending(
        int chunkNumber,
        string content,
        int tokenCount,
        int startOffset,
        int endOffset)
    {
        return new DocumentChunk(
            id: Guid.NewGuid(),
            chunkNumber: chunkNumber,
            content: content,
            tokenCount: tokenCount,
            startOffset: startOffset,
            endOffset: endOffset,
            statusId: 1 // Pending
        );
    }
}