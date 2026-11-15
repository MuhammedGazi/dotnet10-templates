using ProjectApp.Application.Contracts.Persistence;
using ProjectApp.Application.Features.Products.Dto;
using ProjectApp.Domain.Entities;
using ProjectApp.Persistence.Context;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Persistence.Repositories;

public class ProductRepository(AppDbContext context, ISqlConnectionFactory sqlConnectionFactory)
    : GenericRepository<Product>(context), IProductRepository
{
    public Task<List<Product>> GetTopPriceProductsAsync(int count, CancellationToken cancellationToken = default)
    {
        return Context.Products
            .AsNoTracking()
            .OrderByDescending(x => x.Price)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProductDto>> GetTopPricedProductsWithDapperAsync(int count, CancellationToken cancellationToken = default)
    {
        // Seçilen kolonlar ProductDto'nun kurucu parametreleriyle birebir aynı sırada olmalıdır (Dapper record eşlemesi).
        using var connection = sqlConnectionFactory.CreateConnection();
        const string sql = """
            SELECT TOP (@Count) Id, Name, Price, Stock, CategoryId, Created, RowVersion
            FROM Products
            WHERE IsDeleted = 0
            ORDER BY Price DESC
            """;

        var command = new CommandDefinition(sql, new { Count = count }, cancellationToken: cancellationToken);
        var products = await connection.QueryAsync<ProductDto>(command);
        return products.ToList();
    }
}
