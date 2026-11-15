namespace ProjectApp.Repositories.Categories;

public interface ICategoryRepository : IGenericRepository<Category, Guid>
{
    Task<Category?> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default);
}
