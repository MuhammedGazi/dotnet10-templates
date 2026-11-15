using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ProjectApp.Domain.Common;
using ProjectApp.Infrastructure.Persistence.Context;

namespace ProjectApp.UnitTests;

/// <summary>
/// Model kuralı: AuditableEntity'den türeyen her entity'nin RowVersion alanı SQL Server rowversion olarak yapılandırılmalı.
/// Configuration'da <c>IsRowVersion()</c> unutulursa kolon varbinary(max) olur ve sonradan AlterColumn 4927 hatası verir.
/// Model bağlantı açılmadan, yalnızca SQL Server sağlayıcı kurallarıyla kurulur.
/// </summary>
public class ModelConventionTests
{
    [Fact]
    public void AuditableEntityTurevleri_RowVersionSqlServerRowversionOlarakYapilandirilmis()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=.;Database=ModelOnly")
            .Options;
        using var context = new AppDbContext(options);

        var entityTypes = context.Model.GetEntityTypes()
            .Where(e => typeof(AuditableEntity).IsAssignableFrom(e.ClrType))
            .ToList();
        Assert.NotEmpty(entityTypes);

        var errors = new List<string>();
        foreach (var entityType in entityTypes)
        {
            var property = entityType.FindProperty(nameof(AuditableEntity.RowVersion));
            if (property is null || !property.IsConcurrencyToken || property.ValueGenerated != ValueGenerated.OnAddOrUpdate)
            {
                errors.Add($"{entityType.ClrType.Name}: {entityType.ClrType.Name}Configuration'a Property(x => x.RowVersion).IsRowVersion() ekle (migration'dan önce).");
            }
        }

        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
