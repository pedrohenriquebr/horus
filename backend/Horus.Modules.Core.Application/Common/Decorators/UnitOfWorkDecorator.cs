using Horus.Modules.Core.Domain;
using Horus.Modules.Shared.Contracts.SharedKernel;

namespace Horus.Modules.Core.Application.Common.Decorators;

public class UnitOfWorkDecorator<TNotification> : IDomainEventHandler<TNotification>
    where TNotification : BaseEvent
{
    private readonly IDomainEventHandler<TNotification> _innerHandler;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkDecorator(IDomainEventHandler<TNotification> innerHandler, IUnitOfWork unitOfWork)
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