using Hangfire;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Shared.Contracts.SharedKernel;
using MediatR;

namespace Horus.Modules.Core.Application;

public static class MediatorExtensions
{
    public static void EnqueueRequest<T>(this IMediator mediator, T data)
        where T : ICommand
    {
        BackgroundJob.Enqueue<IMediator>(HangFireQueues.Normal, x => x.Send<T>(data, default));
    }


    public static void EnqueueEvent<T>(this IMediator mediator, T data)
        where T : BaseEvent
    {
        BackgroundJob.Enqueue<IMediator>(HangFireQueues.Normal, x => x.Publish<T>(data, default));
    }
}