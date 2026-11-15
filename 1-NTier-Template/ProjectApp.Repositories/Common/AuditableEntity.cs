namespace ProjectApp.Repositories;

/// <summary>
/// Oluşturulma/güncellenme zamanlarını içeren temel entity.
/// Alanlar <c>AuditDbContextInterceptor</c> tarafından otomatik doldurulur.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
}