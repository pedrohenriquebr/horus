using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Events;
using Horus.Modules.Core.Domain.Factories;
using Horus.Modules.Core.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Application.DomainEventsHandlers;

public class DocumentChunkCreatedHandler : INotificationHandler<DocumentChunkCreatedEvent>
{
    public DocumentChunkCreatedHandler(IDocumentsRepository documentsRepository,
        ILogger<DocumentChunkCreatedHandler> logger, IMediator mediator, IDocumentChunkFactory chunkfactory,
        IHorusContext context)
    {
        _documentsRepository = documentsRepository;
        _logger = logger;
        _mediator = mediator;
        _chunkfactory = chunkfactory;
        _context = context;
    }

    private readonly IDocumentsRepository _documentsRepository;
    private readonly ILogger<DocumentChunkCreatedHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IDocumentChunkFactory _chunkfactory;
    private readonly IHorusContext _context;

    public async Task Handle(DocumentChunkCreatedEvent notification, CancellationToken cancellationToken)
    {
        var chunk = await _context.DocumentChunks.FirstOrDefaultAsync(d => d.Id == notification.DocumentChunk.Id);

        if (chunk == null)
        {
            this._logger.LogError("Chunk not found: \n {ChunkId} ", notification.DocumentChunk.Id);
            return;
        }

        try
        {
            chunk.StartEmbedding();
        }
        catch (Exception ex)
        {
            chunk.FailProcessing(ex.Message);
            this._logger.LogError("Error start embedding on chunk: \n {DocumentChunkId} ",
                notification.DocumentChunk.Id);
            throw;
        }
    }
}