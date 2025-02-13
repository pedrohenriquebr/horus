using Horus.Modules.Shared.Contracts.Services;
using MediatR;

namespace Horus.Modules.Shared.Contracts.SharedKernel;

public abstract class BaseEvent : INotification
{
    public BaseEvent()
    {
        DateTimeCreated = DateTimeProvider.Now;
    }

    public DateTime DateTimeCreated { get; }
}