// S1 entegrasyon testlerinde CustomWebApplicationFactory tarafından kullanılır; S1-S2 ve S2-S3 reçetelerinde de yer alır.
using Microsoft.EntityFrameworkCore;
using ProjectApp.Repositories;

namespace ProjectApp.IntegrationTests;

/// <summary>
/// SQLite <c>rowversion</c> üretmez; bu yüzden testlerde RowVersion alanı sıradan bir kolon gibi davranır.
/// Üretim kodu (AppDbContext) değişmez, yalnızca test DbContext'i modeli düzeltir.
/// </summary>
public class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.FindProperty("RowVersion") is null) continue;

            builder.Entity(entityType.ClrType)
                .Property("RowVersion")
                .IsConcurrencyToken(false)
                .ValueGeneratedNever();
        }
    }
}
