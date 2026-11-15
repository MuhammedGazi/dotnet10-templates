using ProjectApp.Application.Contracts.Caching;

namespace ProjectApp.Application.Common;

public static class CacheKeys
{
    public const string Products = "products_all";

    /// <summary>
    /// Dinamik (kayıt başına) anahtar örneği. Kullanmak zorunlu değildir; kullanırsanız o kaydı değiştiren her yol
    /// bu anahtarı da <see cref="ICacheService.RemoveAsync"/> ile temizlemelidir. Negatif önbellekle
    /// (<see cref="ICacheService.GetOrCreateOrNullAsync{T}"/>) birlikte, var olmayan kayıt sorguları da DB'ye gitmez.
    /// </summary>
    public static string ProductById(Guid id) => $"product_{id:N}";
}

public static class CacheInvalidationExtensions
{
    /// <summary>
    /// Ürün listesi önbelleğini temizler. Ürünü (stok dahil) değiştiren HER kod yolu (servis, olay handler'ı,
    /// toplu iş) commit sonrası bunu çağırmalıdır; temizlik tek yerden yapılır. Commit sonrası çağrı
    /// iptal edilmemeli, bu yüzden CancellationToken.None kullanılır.
    /// </summary>
    /// <example>Stok düşen bir olay handler'ı: <c>await unitOfWork.SaveChangesAsync(ct); await cacheService.InvalidateProductsAsync();</c>
    /// (ICacheService handler'ın constructor'ından alınır; handler scoped çalıştığı için singleton servis sorun değildir).</example>
    public static Task InvalidateProductsAsync(this ICacheService cacheService) =>
        cacheService.RemoveAsync(CacheKeys.Products, CancellationToken.None);
}
