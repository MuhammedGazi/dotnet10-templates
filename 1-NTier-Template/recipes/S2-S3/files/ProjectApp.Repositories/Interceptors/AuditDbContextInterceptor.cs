using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ProjectApp.Repositories.Interceptors;

public class AuditDbContextInterceptor(ICurrentUserService currentUserService, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly Dictionary<EntityState, Action<DbContext, IAuditEntity, Guid?, DateTime>> Behaviors = new()
    {
        { EntityState.Added, AddBehavior },
        { EntityState.Modified, ModifiedBehavior },
        { EntityState.Deleted, DeletedBehavior }
    };


    private static void AddBehavior(DbContext context, IAuditEntity auditEntity, Guid? userId, DateTime now)
    {
        auditEntity.Created = now;
        auditEntity.CreatedBy = userId;
        context.Entry(auditEntity).Property(x => x.Updated).IsModified = false;
        context.Entry(auditEntity).Property(x => x.UpdatedBy).IsModified = false;
    }

    private static void ModifiedBehavior(DbContext context, IAuditEntity auditEntity, Guid? userId, DateTime now)
    {
        context.Entry(auditEntity).Property(x => x.Created).IsModified = false;
        context.Entry(auditEntity).Property(x => x.CreatedBy).IsModified = false;
        auditEntity.Updated = now;
        auditEntity.UpdatedBy = userId;
    }

    // soft delete: fiziksel silme yerine IsDeleted = true olarak güncellenir
    private static void DeletedBehavior(DbContext context, IAuditEntity auditEntity, Guid? userId, DateTime now)
    {
        context.Entry(auditEntity).State = EntityState.Modified;
        auditEntity.IsDeleted = true;

        ModifiedBehavior(context, auditEntity, userId, now);
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
        var userId = currentUserService.UserId;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entityEntry in context.ChangeTracker.Entries().ToList())
        {
            if (entityEntry.Entity is not IAuditEntity auditEntity) continue;


            if (entityEntry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;


            Behaviors[entityEntry.State](context, auditEntity, userId, now);
        }
    }
}
