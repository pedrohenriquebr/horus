using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Events;
using Horus.Modules.Core.Domain.Factories;
using Horus.Modules.Core.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Application.DomainEventsHandlers;

public class DocumentChunkEmbeddingStartedEventHandler : INotificationHandler<DocumentChunkEmbeddingStartedEvent>
{
    public DocumentChunkEmbeddingStartedEventHandler(IDocumentsRepository documentsRepository,
        IEmbeddingService embeddingService, ILogger<DocumentChunkEmbeddingStartedEventHandler> logger,
        IMediator mediator, IDocumentChunkFactory chunkfactory, IHorusContext context)
    {
        _documentsRepository = documentsRepository;
        _embeddingService = embeddingService;
        _logger = logger;
        _mediator = mediator;
        _chunkfactory = chunkfactory;
        _context = context;
    }

    private readonly IDocumentsRepository _documentsRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DocumentChunkEmbeddingStartedEventHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IDocumentChunkFactory _chunkfactory;
    private readonly IHorusContext _context;

    public async Task Handle(DocumentChunkEmbeddingStartedEvent notification, CancellationToken cancellationToken)
    {
        var chunk = await _context.DocumentChunks.FirstOrDefaultAsync(d => d.Id == notification.DocumentChunk.Id);

        if (chunk == null)
        {
            this._logger.LogError("Chunk not found: \n {ChunkId} ", notification.DocumentChunk.Id);
            return;
        }

        try
        {
            var embedding = await _embeddingService.GetEmbeddingAsync(chunk.Content);
            EmbeddingModel? defaultModel =
                await _context.EmbeddingModels.FirstOrDefaultAsync(x => x.Name == "all-minilm:l6-v2",
                    cancellationToken);

            if (defaultModel != null)
            {
                chunk.FinishEmbedding(embedding, defaultModel.Id);

                if (chunk.Metadata.TryGetValue("isLastChunk", out var isLastChunk) &&  bool.TryParse(isLastChunk.ToString(), out var isLastChunkBool) && isLastChunkBool)
                {
                    var originalDocument = await _context.Documents.FirstOrDefaultAsync(d => d.Id == chunk.DocumentId, cancellationToken: cancellationToken);
                    if (originalDocument != null)
                    {
                        originalDocument.FinishChunking();
                    }
                }
            }
            else 
                chunk.FailEmbedding("model not found");
        }
        catch (Exception ex)
        {
            chunk.FailEmbedding(ex.Message);
            this._logger.LogError("Error on embedding on chunk: \n {DocumentChunkId} ",
                notification.DocumentChunk.Id);
        }
    }
}