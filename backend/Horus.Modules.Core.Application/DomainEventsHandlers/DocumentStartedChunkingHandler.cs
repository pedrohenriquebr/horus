using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Events;
using Horus.Modules.Core.Domain.Factories;
using Horus.Modules.Core.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Horus.Modules.Core.Application.DomainEventsHandlers;

public class DocumentStartedChunkingHandler : INotificationHandler<DocumentStartedChunking>
{
    public DocumentStartedChunkingHandler(IDocumentsRepository documentsRepository,
        ILogger<DocumentStartedChunkingHandler> logger,
        IMediator mediator,
        IDocumentChunkFactory chunkfactory, 
        IHorusContext context,
        IFixedChunkStrategy fixedChunkStrategy,
        ISemanticChunkStrategy sematicChunkStrategy)
    {
        _documentsRepository = documentsRepository;
        _logger = logger;
        _mediator = mediator;
        _chunkfactory = chunkfactory;
        _context = context;
        _fixedChunkStrategy = fixedChunkStrategy;
        _sematicChunkStrategy = sematicChunkStrategy;
    }

    private readonly IDocumentsRepository _documentsRepository;
    private readonly ILogger<DocumentStartedChunkingHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IDocumentChunkFactory _chunkfactory;
    private readonly IHorusContext _context;
    private readonly IFixedChunkStrategy _fixedChunkStrategy;
    private readonly ISemanticChunkStrategy _sematicChunkStrategy;

    public async Task Handle(DocumentStartedChunking notification, CancellationToken cancellationToken)
    {
        var document = await _documentsRepository.GetAsync(notification.Document.Id);

        if (document == null)
        {
            this._logger.LogError("Document not found: \n {DocumentId} ", notification.Document.Id);
            return;
        }

        try
        {
            var chunkStrategy = document.ChunkingStrategy;
            switch (chunkStrategy)
            {
                case "fixed_chunk":
                    //split the document into chunks
                    foreach (var chunk in _fixedChunkStrategy.SplitChunks(document.Id, document.ProcessedContent))
                    {
                        document.AddChunk(chunk);
                    }
                    break;
                
                case "semantic_chunk":
                    var chunks = _sematicChunkStrategy.SplitChunks(document.Id, document.ProcessedContent).ToList();
                    for (var i = 0; i < chunks.Count; i++)
                    {
                        //if is the last chunk
                        if (i == chunks.Count - 1)
                        {
                            var newMetadata = new Dictionary<string, object>(chunks[i].Metadata);
                            newMetadata.Add("isLastChunk", true);
                            chunks[i].SetMetadata(newMetadata);
                        }
                        document.AddChunk(chunks[i]);
                    }
                    break;
                default:
                    //nothing
                    break;
            }
        }
        catch (Exception ex)
        {
            document.FailProcessing("DOCUMENT_CHUNKING_ERROR", ex.Message);
            this._logger.LogError("Error start chunking document: \n {DocumentId} ", notification.Document.Id);
            throw;
        }
    }


}