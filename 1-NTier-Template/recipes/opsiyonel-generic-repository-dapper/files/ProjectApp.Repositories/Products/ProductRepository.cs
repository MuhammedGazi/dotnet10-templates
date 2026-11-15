using Dapper;
using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Repositories.Products;

public class ProductRepository(AppDbContext context)
    : GenericRepository<Product, Guid>(context), IProductRepository
{
    public Task<List<Product>> GetTopPriceProductsAsync(int count, CancellationToken cancellationToken = default)
    {
        return Context.Products.AsNoTracking().OrderByDescending(x => x.Price).Take(count).ToListAsync(cancellationToken);
    }

    /// <summary>Örnek Dapper sorgusu: fiyata göre ilk N ürün (yalnızca gerekli kolonlar seçilir).</summary>
    public async Task<List<Product>> GetTopPricedProductsWithDapperAsync(int count, CancellationToken cancellationToken = default)
    {
        var connection = Context.Database.GetDbConnection(); // bağlantı DbContext'e aittir, dispose edilmez
        const string sql = """
            SELECT TOP (@Count) Id, Name, Price, Stock, CategoryId, Created, RowVersion
            FROM Products
            WHERE IsDeleted = 0
            ORDER BY Price DESC
            """;
        var command = new CommandDefinition(sql, new { Count = count }, cancellationToken: cancellationToken);
        var products = await connection.QueryAsync<Product>(command);
        return products.AsList();
    }
}
