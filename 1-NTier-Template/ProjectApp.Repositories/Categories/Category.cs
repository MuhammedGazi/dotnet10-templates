using ProjectApp.Repositories.Products;

namespace ProjectApp.Repositories.Categories;

public class Category : AuditableEntity
{
    public string Name { get; set; } = default!;

    public List<Product>? Products { get; set; }
}
