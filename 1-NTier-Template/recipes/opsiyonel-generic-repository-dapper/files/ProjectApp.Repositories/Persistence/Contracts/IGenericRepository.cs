using System.Linq.Expressions;

namespace ProjectApp.Repositories;

public interface IGenericRepository<T, TId> where T : class where TId : struct
{
    Task<bool> AnyAsync(TId id, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    IQueryable<T> GetAll();
    /// <summary>[Hazır altyapı] Koşula göre izlenmeyen (AsNoTracking) sorgu döner; özel filtreleme için kullanın.</summary>
    IQueryable<T> Where(Expression<Func<T, bool>> predicate);
    ValueTask<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    ValueTask AddAsync(T entity, CancellationToken cancellationToken = default);
    /// <summary>
    /// İyimser eşzamanlılık için istemcinin gönderdiği RowVersion'ı orijinal değer olarak ayarlar;
    /// SaveChanges sırasında kayıt bu arada değiştiyse DbUpdateConcurrencyException (409) oluşur.
    /// Yalnızca RowVersion içeren (AuditableEntity) varlıklarda kullanılır.
    /// </summary>
    void SetOriginalRowVersion(T entity, byte[] rowVersion);
    void Update(T entity);
    void Delete(T entity);
}
