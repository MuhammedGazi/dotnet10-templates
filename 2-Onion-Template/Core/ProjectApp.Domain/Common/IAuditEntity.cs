namespace ProjectApp.Domain.Common;

public interface IAuditEntity
{
    DateTime Created { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? Updated { get; set; }
    Guid? UpdatedBy { get; set; }
    bool IsDeleted { get; set; }
}
