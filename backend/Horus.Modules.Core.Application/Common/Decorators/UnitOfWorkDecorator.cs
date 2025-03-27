using System.Data;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Common.Decorators;

public class UnitOfWorkDecorator<TNotification> : INotificationHandler<TNotification>
    where TNotification : BaseEvent
{
    private readonly INotificationHandler<TNotification> _innerHandler;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkDecorator(INotificationHandler<TNotification> innerHandler, IUnitOfWork unitOfWork)
    {
        _innerHandler = innerHandler;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _unitOfWork.BeginTransaction();
            await _innerHandler.Handle(notification, cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception)
        {

            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}