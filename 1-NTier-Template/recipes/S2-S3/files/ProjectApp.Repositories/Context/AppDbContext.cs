using System.Linq.Expressions;
using ProjectApp.Repositories.Categories;
using ProjectApp.Repositories.Identity;
using ProjectApp.Repositories.Products;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Repositories;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<Category> Categories { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IdentityDbContext tablolarının konfigürasyonu önce uygulanmalı,
        // böylece aşağıdaki özel konfigürasyonlar gerekirse onları ezebilir.
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(RepositoryAssembly).Assembly);

        // AppUser Soft Delete global query filter
        builder.Entity<AppUser>().HasQueryFilter(u => !u.IsDeleted);

        ApplySoftDeleteQueryFilter(builder);
    }

    // IAuditEntity uygulayan tüm entity'ler için: x => !x.IsDeleted
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
