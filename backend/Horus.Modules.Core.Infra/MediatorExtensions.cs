using Horus.Modules.Core.Application;
using Horus.Modules.Core.Infra.Context;
using Horus.Modules.Shared.Contracts.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Infra;

public static class MediatorExtensions
{
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, HorusContext context)
    {
        var entities = context.ChangeTracker.Entries<IEntity>()
            .Where(e => e.State != EntityState.Detached
                        && e.Entity.DomainEvents != null
                        && e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();


        var domainEvents = entities
            .SelectMany(d => d.DomainEvents)
            .ToList();

        entities
            .ForEach(e => e.ClearDomainEvents());

        foreach (var @event in domainEvents)
            mediator.EnqueueEvent(@event);
    }
}