using ProjectApp.Application.Contracts.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ProjectApp.Infrastructure.Caching;

/// <summary>
/// Süreç içi (IMemoryCache) önbellek. Çoklu instance'ta her instance kendi kopyasını tutar; yazma sonrası
/// temizlik yalnızca isteği alan instance'ta olur. Ölçeklemede dağıtık önbellek (Redis / HybridCache) kullanın.
/// Uygulamanın paylaşılan IMemoryCache'inden ayrı, boyut sınırlı kendi örneğini kullanır
/// (paylaşılan önbelleğe SizeLimit konursa Size vermeyen diğer kütüphaneler istisna fırlatır).
/// </summary>
public sealed class CacheService : ICacheService, IDisposable
{
    private const int DefaultSizeLimit = 1024;

    // Şeritli kilit: anahtarın hash'iyle seçilen sabit sayıda SemaphoreSlim. Dinamik anahtarlarda (ürün başına vb.)
    // kilit sayısı büyümez, temizlik gerekmez. İki farklı anahtar aynı şeride düşerse yalnızca birbirini kısa süre
    // bekler (doğruluk bozulmaz, küçük bir paralellik kaybı olur).
    private const int LockStripeCount = 32;
    private static readonly TimeSpan DefaultNullExpiration = TimeSpan.FromSeconds(30);

    // "Kayıt yok" sonucunu temsil eder (MemoryCache'te null değer, yokluktan ayırt edilemez).
    private sealed class NullMarker
    {
        public static readonly NullMarker Instance = new();
    }

    private readonly MemoryCache _memoryCache;
    private readonly SemaphoreSlim[] _locks = CreateLocks();

    public CacheService(IConfiguration configuration)
    {
        // Her girdi 1 birim sayılır; sınır = en fazla tutulacak anahtar sayısı (Caching:SizeLimit).
        var sizeLimit = configuration.GetValue("Caching:SizeLimit", DefaultSizeLimit);
        _memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit });
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        TryGetCached<T>(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        _memoryCache.Set(key, value, CreateEntryOptions(expiration));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        // Null sonuç saklanmaz (nullExpiration = null); bu sözleşme T'nin null dönmemesini bekler.
        var result = await GetOrCreateCoreAsync<T>(key, async ct => await factory(ct), expiration, nullExpiration: null, cancellationToken);
        return result!;
    }

    public Task<T?> GetOrCreateOrNullAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, TimeSpan? nullExpiration = null, CancellationToken cancellationToken = default)
        where T : class =>
        GetOrCreateCoreAsync(key, factory, expiration, nullExpiration ?? DefaultNullExpiration, cancellationToken);

    private async Task<T?> GetOrCreateCoreAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration, TimeSpan? nullExpiration, CancellationToken cancellationToken)
    {
        if (TryGetCached<T>(key, out var cached))
        {
            return cached;
        }

        // Şerit kilidi: önbellek boşken gelen eşzamanlı istekler veritabanına tek sorgu gönderir.
        var keyLock = _locks[GetStripeIndex(key)];
        await keyLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCached<T>(key, out cached))
            {
                return cached;
            }

            var value = await factory(cancellationToken);
            if (value is not null)
            {
                _memoryCache.Set(key, value, CreateEntryOptions(expiration));
            }
            else if (nullExpiration.HasValue)
            {
                _memoryCache.Set(key, NullMarker.Instance, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = nullExpiration.Value });
            }

            return value;
        }
        finally
        {
            keyLock.Release();
        }
    }

    // true: anahtar için geçerli bir sonuç var (değer ya da saklanmış null). false: önbellekte yok / tip uymuyor.
    private bool TryGetCached<T>(string key, out T? value)
    {
        value = default;
        if (!_memoryCache.TryGetValue(key, out var entry))
        {
            return false;
        }

        if (entry is NullMarker)
        {
            return true;
        }

        if (entry is T typed)
        {
            value = typed;
            return true;
        }

        return false;
    }

    private static int GetStripeIndex(string key) => (key.GetHashCode() & int.MaxValue) % LockStripeCount;

    private static SemaphoreSlim[] CreateLocks()
    {
        var locks = new SemaphoreSlim[LockStripeCount];
        for (var i = 0; i < locks.Length; i++)
        {
            locks[i] = new SemaphoreSlim(1, 1);
        }

        return locks;
    }

    private static MemoryCacheEntryOptions CreateEntryOptions(TimeSpan? expiration)
    {
        var options = new MemoryCacheEntryOptions { Size = 1 };
        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }
        else
        {
            options.SetSlidingExpiration(TimeSpan.FromMinutes(10));
        }

        return options;
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
        foreach (var keyLock in _locks)
        {
            keyLock.Dispose();
        }
    }
}
