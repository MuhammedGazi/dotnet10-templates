namespace ProjectApp.Repositories;

/// <summary>
/// Denetim alanları ve soft delete içeren temel entity.
/// Alanlar <c>AuditDbContextInterceptor</c> tarafından otomatik doldurulur.
/// </summary>
public abstract class AuditableEntity : BaseEntity, IAuditEntity
{
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Optimistic concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = [];
}
