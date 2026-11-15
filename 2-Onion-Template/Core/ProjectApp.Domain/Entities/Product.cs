using ProjectApp.Domain.Common;

namespace ProjectApp.Domain.Entities;

public class Product : AuditableEntity
{
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;
}
