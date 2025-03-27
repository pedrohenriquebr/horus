using Grpc.Core;
using Horus.Modules.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Horus.Modules.Core.Infra.Context.Interceptors;

public class UpdateAuditableEntities : SaveChangesInterceptor
{
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        Update(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void Update(DbContext? eventDataContext)
    {
        DbContext? context = eventDataContext;
        
        if (context == null) return;

        var entries = context.ChangeTracker
            .Entries<IAuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                entityEntry.Property(d => d.CreatedAt).CurrentValue = DateTime.UtcNow;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entityEntry.Property(d => d.UpdatedAt).CurrentValue = DateTime.UtcNow;
            }
        }
    }
}