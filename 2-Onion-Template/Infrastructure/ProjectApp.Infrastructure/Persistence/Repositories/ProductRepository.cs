using ProjectApp.Application.Contracts.Persistence;
using ProjectApp.Domain.Entities;
using ProjectApp.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Infrastructure.Persistence.Repositories;

public class ProductRepository(AppDbContext context) : IProductRepository
{
    public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        context.Products.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await context.Products.CountAsync(cancellationToken);

        // SQL Server uniqueidentifier'ı son byte'lardan sıraladığı için Guid v7 kronolojik değildir;
        // tutarlı ve kronolojik sayfalama için önce Created, sonra Id'ye göre sıralanır.
        var items = await context.Products
            .AsNoTracking()
            .OrderBy(x => x.Created).ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);

    public Task<bool> AnyInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(x => x.CategoryId == categoryId, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);

    public void Remove(Product product) => context.Products.Remove(product);

    public void SetOriginalRowVersion(Product product, byte[] rowVersion) =>
        context.Entry(product).Property(x => x.RowVersion).OriginalValue = rowVersion;
}
