using System.Reflection;
using Microsoft.Extensions.Configuration;
using ProjectApp.Application.Common;
using ProjectApp.Infrastructure.Caching;

namespace ProjectApp.UnitTests;

public class CacheServiceTests
{
    private static CacheService CreateService(int sizeLimit = 1024) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Caching:SizeLimit"] = sizeLimit.ToString(System.Globalization.CultureInfo.InvariantCulture) })
            .Build());

    private static int LockCount(CacheService service) =>
        ((Array)typeof(CacheService).GetField("_locks", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service)!).Length;

    [Fact]
    public async Task GetOrCreateAsync_AyniAnahtarEszamanli_FactoryBirKezCalisir()
    {
        using var service = CreateService();
        var calls = 0;

        async Task<string> Factory(CancellationToken ct)
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(50, ct);
            return "deger";
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => service.GetOrCreateAsync("k", Factory, cancellationToken: TestContext.Current.CancellationToken)));

        Assert.Equal(1, calls);
        Assert.All(results, r => Assert.Equal("deger", r));
    }

    [Fact]
    public async Task GetOrCreateAsync_DinamikAnahtarlar_KilitSayisiSabitKalir()
    {
        using var service = CreateService(sizeLimit: 10);
        var before = LockCount(service);

        for (var i = 0; i < 5000; i++)
        {
            await service.GetOrCreateAsync(CacheKeys.ProductById(Guid.CreateVersion7()), _ => Task.FromResult("x"),
                TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        }

        Assert.Equal(before, LockCount(service));
    }

    [Fact]
    public async Task GetOrCreateAsync_FarkliAnahtarlar_BirbiriniKilitlemez()
    {
        using var service = CreateService();
        var release = new TaskCompletionSource();

        // Çok sayıda anahtar için factory'ler bloklu; biri bile şerit paylaşsa kalan işler yine de bitmeli.
        var blocked = service.GetOrCreateAsync("bekleyen", async _ => { await release.Task; return "a"; }, cancellationToken: TestContext.Current.CancellationToken);
        var results = new List<string>();
        for (var i = 0; i < 50; i++)
        {
            var key = $"anahtar-{i}";
            // Aynı şeride düşenler bloklanabilir; bu yüzden zaman aşımıyla yalnızca tamamlananlar sayılır.
            var task = service.GetOrCreateAsync(key, _ => Task.FromResult(key), cancellationToken: TestContext.Current.CancellationToken);
            if (await Task.WhenAny(task, Task.Delay(100, TestContext.Current.CancellationToken)) == task) results.Add(await task);
        }

        release.SetResult();
        Assert.Equal("a", await blocked);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetOrCreateAsync_NullDonerse_Saklanmaz()
    {
        using var service = CreateService();
        var calls = 0;
        Task<string> Factory(CancellationToken _)
        {
            calls++;
            return Task.FromResult<string>(null!);
        }

        await service.GetOrCreateAsync("k", Factory, cancellationToken: TestContext.Current.CancellationToken);
        await service.GetOrCreateAsync("k", Factory, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetOrCreateOrNullAsync_NullSonuc_NegatifOnbellegeAlinir()
    {
        using var service = CreateService();
        var calls = 0;
        Task<string?> Factory(CancellationToken _)
        {
            calls++;
            return Task.FromResult<string?>(null);
        }

        var first = await service.GetOrCreateOrNullAsync("yok", Factory, cancellationToken: TestContext.Current.CancellationToken);
        var second = await service.GetOrCreateOrNullAsync("yok", Factory, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrCreateOrNullAsync_NegatifSureDolunca_FactoryYenidenCalisir()
    {
        using var service = CreateService();
        var calls = 0;
        Task<string?> Factory(CancellationToken _)
        {
            calls++;
            return Task.FromResult<string?>(null);
        }

        await service.GetOrCreateOrNullAsync("yok", Factory, nullExpiration: TimeSpan.FromMilliseconds(100), cancellationToken: TestContext.Current.CancellationToken);
        await Task.Delay(400, TestContext.Current.CancellationToken);
        await service.GetOrCreateOrNullAsync("yok", Factory, nullExpiration: TimeSpan.FromMilliseconds(100), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetOrCreateOrNullAsync_NegatifSonuc_RemoveSonrasiYenidenUretilir()
    {
        using var service = CreateService();
        var exists = false;
        Task<string?> Factory(CancellationToken _) => Task.FromResult(exists ? "var" : null);

        Assert.Null(await service.GetOrCreateOrNullAsync("k", Factory, cancellationToken: TestContext.Current.CancellationToken));
        exists = true;
        await service.RemoveAsync("k", TestContext.Current.CancellationToken);

        Assert.Equal("var", await service.GetOrCreateOrNullAsync("k", Factory, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvalidateProductsAsync_UrunListesiAnahtariniTemizler()
    {
        using var service = CreateService();
        await service.SetAsync(CacheKeys.Products, "liste", cancellationToken: TestContext.Current.CancellationToken);
        await service.SetAsync("baska", "deger", cancellationToken: TestContext.Current.CancellationToken);

        await service.InvalidateProductsAsync();

        Assert.Null(await service.GetAsync<string>(CacheKeys.Products, TestContext.Current.CancellationToken));
        Assert.Equal("deger", await service.GetAsync<string>("baska", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAsync_NegatifIsaretci_NullDoner()
    {
        using var service = CreateService();
        await service.GetOrCreateOrNullAsync<string>("yok", _ => Task.FromResult<string?>(null), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(await service.GetAsync<string>("yok", TestContext.Current.CancellationToken));
    }
}
