using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Repositories.Categories;

public class CategoryRepository(AppDbContext context)
    : GenericRepository<Category, Guid>(context), ICategoryRepository
{
    public Task<Category?> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Context.Categories.AsNoTracking().Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
