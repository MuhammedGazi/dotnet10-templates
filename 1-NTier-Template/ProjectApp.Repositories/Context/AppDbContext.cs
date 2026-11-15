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
    }
}
