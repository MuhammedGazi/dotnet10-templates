using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace ProjectApp.IntegrationTests;

/// <summary>
/// Global limit küçükken kotası biten IP'nin login'i 429 almaz (auth-policy uçları global limitten muaf);
/// auth dışı uçlar ise global limite tabi kalır.
/// </summary>
public class AuthLimiterExemptTests : IClassFixture<AuthLimiterExemptTests.SmallGlobalLimitFactory>
{
    private const string Api = "/api";
    private readonly SmallGlobalLimitFactory _factory;

    public AuthLimiterExemptTests(SmallGlobalLimitFactory factory) => _factory = factory;

    // Ayrı factory örneği (ayrı bellek içi DB ve limiter kovaları); ortam değişkenlerine dokunmaz.
    public sealed class SmallGlobalLimitFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // UseSetting, WebApplicationBuilder yapılandırması okunmadan önce uygulanır ve
            // CustomWebApplicationFactory'nin process genelindeki ortam değişkenini ezer.
            builder.UseSetting("RateLimiting:Fixed:PermitLimit", "3");
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "2");
            base.ConfigureWebHost(builder);
        }
    }

    [Fact]
    public async Task GlobalKotasiBitenIp_LoginOlabilir_AmaGetHala429Doner()
    {
        using var client = _factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            var r = await client.GetAsync($"{Api}/products", TestContext.Current.CancellationToken);
            statuses.Add(r.StatusCode);
        }
        // Ayarın env var'ı gerçekten ezdiğinin kanıtı: 4. istek global limite takılmış olmalı.
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);

        var login = await client.PostAsJsonAsync($"{Api}/auth/login",
            new { email = "yok@example.com", password = "YanlisSifre12*" }, TestContext.Current.CancellationToken);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        // me auth-policy taşımaz: global limite tabi kalır (yetkisiz istek 401 değil 429 alır).
        var me = await client.GetAsync($"{Api}/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, me.StatusCode);

        var again = await client.GetAsync($"{Api}/products", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, again.StatusCode);

        // Brute-force koruması zayıflamadı: auth-policy (2/dk) hâlâ uygulanır; 3. login 429.
        var login2 = await client.PostAsJsonAsync($"{Api}/auth/login",
            new { email = "yok@example.com", password = "YanlisSifre12*" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, login2.StatusCode);
        var login3 = await client.PostAsJsonAsync($"{Api}/auth/login",
            new { email = "yok@example.com", password = "YanlisSifre12*" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, login3.StatusCode);
    }
}
