using FluentValidation;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Usecases.ClearDocumentsByUserId;

public class ClearDocumentsByUserIdCommand :ICommand
{
    public Guid UserId { get; set; }
}


public class ClearDocumentsByUserIdCommandValidator : AbstractValidator<ClearDocumentsByUserIdCommand>
{
    public ClearDocumentsByUserIdCommandValidator()
    {
        RuleFor(d => d.UserId)
            .NotEmpty()
            .NotNull();
    }
}


public class ClearDocumentsByUserIdCommandHandler : ICommandHandler<ClearDocumentsByUserIdCommand>
{
    private readonly IDocumentsRepository _documentsRepository;


    public ClearDocumentsByUserIdCommandHandler(IDocumentsRepository documentsRepository)
    {
        _documentsRepository = documentsRepository;
    }
    public async Task Handle(ClearDocumentsByUserIdCommand request, CancellationToken cancellationToken)
    {
        await _documentsRepository.DeleteAllDocumentsByUserId(request.UserId.ToString());
    }
}