using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ProjectApp.Repositories.Interceptors;

public class AuditDbContextInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static void AddBehavior(DbContext context, AuditableEntity auditEntity, DateTime now)
    {
        auditEntity.Created = now;
        context.Entry(auditEntity).Property(x => x.Updated).IsModified = false;
    }

    private static void ModifiedBehavior(DbContext context, AuditableEntity auditEntity, DateTime now)
    {
        context.Entry(auditEntity).Property(x => x.Created).IsModified = false;
        auditEntity.Updated = now;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditBehaviors(eventData.Context!);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new())
    {
        ApplyAuditBehaviors(eventData.Context!);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditBehaviors(DbContext context)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entityEntry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entityEntry.State)
            {
                case EntityState.Added:
                    AddBehavior(context, entityEntry.Entity, now);
                    break;
                case EntityState.Modified:
                    ModifiedBehavior(context, entityEntry.Entity, now);
                    break;
            }
        }
    }
}
