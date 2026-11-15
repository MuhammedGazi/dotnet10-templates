using ProjectApp.Application.Features.Products.Dto;
using ProjectApp.Domain.Entities;

namespace ProjectApp.Application.Contracts.Persistence;

public interface IProductRepository : IGenericRepository<Product>
{
    /// <summary>[Hazır örnek] EF Core ile en pahalı N ürün; aynı sorgunun Dapper karşılığıyla karşılaştırma için tutulur (uç bağlı değil).</summary>
    Task<List<Product>> GetTopPriceProductsAsync(int count, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> GetTopPricedProductsWithDapperAsync(int count, CancellationToken cancellationToken = default);
}
