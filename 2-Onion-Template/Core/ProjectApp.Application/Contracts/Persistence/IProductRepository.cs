using ProjectApp.Domain.Entities;

namespace ProjectApp.Application.Contracts.Persistence;

/// <summary>
/// Ürün verisine erişim. Yalnızca servislerin gerçekten kullandığı işlemler bulunur; yeni ihtiyaçta
/// buraya amaca özel bir metot eklenir (genel sorgu nesnesi dışarı açılmaz).
/// </summary>
public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Değişiklikleri izlenen kaydı döner (güncelleme/silme için); bulunamazsa null.</summary>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> AnyInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    void Add(Product product);
    void Remove(Product product);

    /// <summary>
    /// İyimser eşzamanlılık: istemcinin gönderdiği RowVersion orijinal değer olur; kayıt bu arada
    /// değiştiyse SaveChanges sırasında DbUpdateConcurrencyException (409) oluşur.
    /// </summary>
    void SetOriginalRowVersion(Product product, byte[] rowVersion);
}
