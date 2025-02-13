using MediatR;

namespace Horus.Modules.Core.Application.Events;

public abstract record BaseApplicationEvent : INotification
{
    public BaseApplicationEvent()
    {
        DateTimeCreated = DateTime.Now;
    }

    public DateTime DateTimeCreated { get; init; }
}