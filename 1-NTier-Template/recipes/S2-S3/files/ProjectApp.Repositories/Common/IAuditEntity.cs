namespace ProjectApp.Repositories;

public interface IAuditEntity
{
    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    // soft delete
    public bool IsDeleted { get; set; }
}
