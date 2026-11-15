namespace ProjectApp.Application.Contracts.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Değer önbellekte yoksa <paramref name="factory"/> ile üretip saklar. Aynı anahtar için eşzamanlı isteklerde
    /// factory yalnızca bir kez çalışır (cache stampede koruması); diğerleri üretilen değeri bekler.
    /// Factory null dönerse sonuç önbelleğe alınmaz; "kayıt yok" sonucunu da saklamak için
    /// <see cref="GetOrCreateOrNullAsync{T}"/> kullanın.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <see cref="GetOrCreateAsync{T}"/> gibidir, ama factory null dönebilir (ör. var olmayan kayıt). Null sonuç
    /// <paramref name="nullExpiration"/> (varsayılan 30 sn) süreyle saklanır (negatif önbellek): aynı anahtar için
    /// tekrarlanan istekler bu sürede DB'ye gitmez. Kaydı oluşturan kod ilgili anahtarı RemoveAsync ile temizlemelidir.
    /// </summary>
    Task<T?> GetOrCreateOrNullAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, TimeSpan? nullExpiration = null, CancellationToken cancellationToken = default)
        where T : class;
}
