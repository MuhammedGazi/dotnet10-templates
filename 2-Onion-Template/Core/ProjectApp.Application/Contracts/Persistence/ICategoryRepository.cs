using ProjectApp.Domain.Entities;

namespace ProjectApp.Application.Contracts.Persistence;

/// <summary>Kategori verisine erişim. Yalnızca servislerin kullandığı işlemler bulunur.</summary>
public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Değişiklikleri izlenen kaydı döner (güncelleme/silme için); bulunamazsa null.</summary>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryWithProductsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    void Add(Category category);
    void Remove(Category category);

    /// <summary>İyimser eşzamanlılık (bkz. IProductRepository.SetOriginalRowVersion).</summary>
    void SetOriginalRowVersion(Category category, byte[] rowVersion);
}
