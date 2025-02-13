using Horus.Modules.Shared.Contracts.SharedKernel;
using MediatR;

namespace Horus.Modules.Core.Application.Common;

// public abstract class DomainEventHandler<TEvent> : INotificationHandler<TEvent>
//     where TEvent : BaseEvent
// {
//     public abstract Task Handle(TEvent @event, CancellationToken cancellationToken);
// }

public interface IDomainEventHandler<TEvent> : INotificationHandler<TEvent>
    where TEvent : BaseEvent
{
}