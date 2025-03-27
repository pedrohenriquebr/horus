using MediatR;

namespace Horus.Modules.Core.Domain.Events;

public abstract class BaseEvent : INotification
{
    public DateTime DateTimeCreated { get; }

    public BaseEvent()
    {
        DateTimeCreated = DateTime.Now;
    }
}