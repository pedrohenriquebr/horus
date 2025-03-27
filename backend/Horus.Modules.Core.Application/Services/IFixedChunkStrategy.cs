using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Application.Services;

public interface IFixedChunkStrategy
{
    IEnumerable<DocumentChunk> SplitChunks(Guid docId, string documentProcessedContent);
}

public interface ISemanticChunkStrategy
{
    IEnumerable<DocumentChunk> SplitChunks(Guid docId, string documentProcessedContent);
}
