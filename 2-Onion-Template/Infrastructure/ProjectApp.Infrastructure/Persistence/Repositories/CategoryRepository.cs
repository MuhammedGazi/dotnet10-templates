using ProjectApp.Application.Contracts.Persistence;
using ProjectApp.Domain.Entities;
using ProjectApp.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Infrastructure.Persistence.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        context.Categories.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Category?> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Categories
            .AsNoTracking()
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Categories.AnyAsync(x => x.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        context.Categories.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);

    public void Add(Category category) => context.Categories.Add(category);

    public void Remove(Category category) => context.Categories.Remove(category);

    public void SetOriginalRowVersion(Category category, byte[] rowVersion) =>
        context.Entry(category).Property(x => x.RowVersion).OriginalValue = rowVersion;
}
