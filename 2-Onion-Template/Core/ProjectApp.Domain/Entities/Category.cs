using ProjectApp.Domain.Common;

namespace ProjectApp.Domain.Entities;

public class Category : AuditableEntity
{
    public string Name { get; set; } = default!;

    public List<Product> Products { get; set; } = [];
}
