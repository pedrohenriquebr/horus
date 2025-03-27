using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Factories;
using Microsoft.Extensions.Options;

namespace Horus.Modules.Core.Infra.Services.RAG;

public class FixedChunkStrategy  : IFixedChunkStrategy
{
    private readonly IDocumentChunkFactory _chunkfactory;
    private readonly IOptions<RagOptions> _options;

    public FixedChunkStrategy(IDocumentChunkFactory chunkfactory, IOptions<RagOptions> options)
    {
        _chunkfactory = chunkfactory;
        _options = options;
    }
    
    public IEnumerable<DocumentChunk> SplitChunks(Guid docId, string documentProcessedContent)
    {
        var chunkSize = this._options.Value.ChunkStrategies.FixedChunk.ChunkSize;
        var chunkOverlap = this._options.Value.ChunkStrategies.FixedChunk.Overlap;
        var chunks = new List<DocumentChunk>();
        int chunkNumber = 1;
        var step = chunkSize - chunkOverlap;

        for (int i = 0; i < documentProcessedContent.Length; i += step)
        {
            // Calculate the maximum possible length from position i
            int chunkLength = Math.Min(chunkSize, documentProcessedContent.Length - i);
            var chunkStr = documentProcessedContent.Substring(i, chunkLength);
            // TODO: calculate the token count for the chunk using llm
            int tokenCount = 10;
            int startOffset = i;
            int endOffset = i + chunkLength;
            var chunk = this._chunkfactory.CreatePending(docId,
                chunkNumber,
                chunkStr,
                tokenCount,
                startOffset,
                endOffset);
            chunks.Add(chunk);
            chunkNumber++;
            
            bool isLastChunk = i + chunkSize >= documentProcessedContent.Length;
            
            chunk.SetMetadata(new  Dictionary<string, object>()
            {
                ["isLastChunk"] =  isLastChunk
            });
        }

        return chunks;
    }
}