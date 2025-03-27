using System.Collections;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Events;
using Horus.Modules.Core.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Application.DomainEventsHandlers;

public class DocumentAddedEventHandler : INotificationHandler<DocumentAddedEvent>
{
    private readonly IDocumentsRepository _documentsRepository;
    private readonly ILogger<DocumentAddedEventHandler> _logger;
    private readonly IMediator _mediator;

    public DocumentAddedEventHandler(IDocumentsRepository documentsRepository,
        ILogger<DocumentAddedEventHandler> logger
        , IMediator mediator)
    {
        _documentsRepository = documentsRepository;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Handle(DocumentAddedEvent notification, CancellationToken cancellationToken)
    {
        var document = await _documentsRepository.GetAsync(notification.Document.Id);

        if (document == null)
        {
            this._logger.LogError("Document not found: \n {DocumentId} ", notification.Document.Id);
            return;
        }
        
        if(document.StatusId != (int)DocumentStatusEnum.Pending)
            return;

        try
        {
            document.ProcessContent(ProcessContent(document.RawContent));
            document.StartProcessing();
        }
        catch (Exception ex)
        {
            document.FailProcessing("DOCUMENT_START_PROCESSING_ERROR", ex.Message);
            this._logger.LogError("Error processing document: \n {DocumentId} ", notification.Document.Id);
            throw;
        }
    }


    //TODO: implement this method
    private string ProcessContent(string documentRawContent)
    {
        //later i need to add some logic here to process the document content
        return documentRawContent.Trim();
    }
}