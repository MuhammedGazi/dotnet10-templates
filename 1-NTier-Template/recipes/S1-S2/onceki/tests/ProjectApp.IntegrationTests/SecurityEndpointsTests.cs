using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ProjectApp.IntegrationTests;

/// <summary>
/// Şifre değiştirme, hesap kilitleme (lockout), global rate limit ve girdi doğrulama testleri.
/// </summary>
public class SecurityEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    // Kilit eşiği koddan okunur; MaxFailedAccessAttempts değişince test kırılmaz.
    private int MaxFailedAttempts =>
        factory.Services.GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;

    private const string Api = "/api";

    [Fact]
    public async Task SifreDegisikligi_DogruMevcutSifre_204DonerVeYeniSifreyleGirisCalisir()
    {
        using var client = factory.CreateClient();
        var (email, tokens) = await factory.CreateUserAndLoginAsync(client, Api);
        client.Authorize(tokens.AccessToken);
        const string newPassword = "YeniSifre12*";

        var change = await client.PostAsJsonAsync($"{Api}/users/change-password",
            new { currentPassword = TestHelpers.Password, newPassword }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        // Şifre değişikliği security stamp'i yeniler: eski access token anında geçersizdir.
        var meWithOldToken = await client.GetAsync($"{Api}/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, meWithOldToken.StatusCode);

        var oldLogin = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = newPassword }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task SifreDegisikligi_YanlisMevcutSifre_400Doner()
    {
        using var client = factory.CreateClient();
        var (_, tokens) = await factory.CreateUserAndLoginAsync(client, Api);
        client.Authorize(tokens.AccessToken);

        var change = await client.PostAsJsonAsync($"{Api}/users/change-password",
            new { currentPassword = "Yanlis12*Sifre", newPassword = "YeniSifre12*" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
    }

    [Fact]
    public async Task Login_BesYanlisDenemedenSonra_DogruSifreDe401Doner()
    {
        using var client = factory.CreateClient();
        var (email, _) = await factory.CreateUserAndLoginAsync(client, Api);

        for (var i = 0; i < MaxFailedAttempts; i++)
        {
            var wrong = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = "Yanlis12*Sifre" }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        var correct = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
    }

    [Fact]
    public async Task GlobalRateLimit_LimitAsildiginda_429Doner()
    {
        using var rateLimitedFactory = factory.WithWebHostBuilder(
            builder => builder.UseSetting("RateLimiting:Fixed:PermitLimit", "2"));
        using var client = rateLimitedFactory.CreateClient();

        var first = await client.GetAsync($"{Api}/categories", TestContext.Current.CancellationToken);
        var second = await client.GetAsync($"{Api}/categories", TestContext.Current.CancellationToken);
        var third = await client.GetAsync($"{Api}/categories", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.True(third.Headers.RetryAfter is not null);
    }

    [Theory]
    [InlineData("100000000000000000")] // decimal(18,2) taşması
    [InlineData("10.999")]             // 2'den fazla ondalık
    public async Task UrunOlustur_GecersizFiyatHassasiyeti_400Doner(string price)
    {
        using var client = factory.CreateClient();
        client.Authorize(await factory.GetAdminAccessTokenAsync(client, Api));
        var categoryId = await CreateCategoryAsync(client);

        using var content = new StringContent(
            $$"""{"name":"Fiyat {{Guid.NewGuid():N}}","price":{{price}},"stock":1,"categoryId":"{{categoryId}}"}""",
            System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{Api}/products", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<Guid> CreateCategoryAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync($"{Api}/categories", new { name = $"Kat{Guid.NewGuid():N}"[..20] }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.ReadDataAsync<IdResponse>()).Id;
    }
}
