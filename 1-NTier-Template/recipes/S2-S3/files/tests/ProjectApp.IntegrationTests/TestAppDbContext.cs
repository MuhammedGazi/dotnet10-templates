using Microsoft.EntityFrameworkCore;
using ProjectApp.Repositories;
namespace ProjectApp.IntegrationTests;

/// <summary>
/// SQLite <c>rowversion</c> üretmez. Bu context RowVersion'ı concurrency token olarak tutar ve her kayıtta
/// yeni bir değer üretir; böylece iyimser eşzamanlılık kontrolü (409) SQLite'ta da gerçekten çalışır.
/// Üretim kodu (AppDbContext) değişmez, yalnızca test DbContext'i modeli düzeltir.
/// </summary>
public class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.FindProperty(nameof(AuditableEntity.RowVersion)) is null) continue;

            builder.Entity(entityType.ClrType)
                .Property(nameof(AuditableEntity.RowVersion))
                .IsConcurrencyToken()
                .ValueGeneratedNever();
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Eklenen/değişen/silinen (soft delete'e çevrilecek) her AuditableEntity için yeni RowVersion üretir.
    // Orijinal değer istemcinin gönderdiği değerdir; WHERE koşulu onunla çalışır.
    private void StampRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                entry.Property(x => x.RowVersion).CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
