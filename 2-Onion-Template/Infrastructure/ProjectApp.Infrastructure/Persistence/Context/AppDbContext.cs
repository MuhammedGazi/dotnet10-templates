using System.Linq.Expressions;
using System.Reflection;
using ProjectApp.Domain.Common;
using ProjectApp.Domain.Entities;
using ProjectApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Infrastructure.Persistence.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<Category> Categories { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Entity<AppUser>().HasQueryFilter(u => !u.IsDeleted);
        ApplySoftDeleteQueryFilter(builder);
    }

    private static void ApplySoftDeleteQueryFilter(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(IAuditEntity).IsAssignableFrom(entityType.ClrType)) continue;
            if (entityType.IsOwned()) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "x");
            var isDeletedProperty = Expression.Property(parameter, nameof(IAuditEntity.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(isDeletedProperty), parameter);

            builder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }
}
