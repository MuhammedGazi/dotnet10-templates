namespace ProjectApp.Repositories.Products;

public interface IProductRepository : IGenericRepository<Product, Guid>
{
    /// <summary>[Hazır örnek] EF Core ile en pahalı N ürün; aynı sorgunun Dapper karşılığıyla karşılaştırma için tutulur (uç bağlı değil).</summary>
    Task<List<Product>> GetTopPriceProductsAsync(int count, CancellationToken cancellationToken = default);
    Task<List<Product>> GetTopPricedProductsWithDapperAsync(int count, CancellationToken cancellationToken = default);
}
