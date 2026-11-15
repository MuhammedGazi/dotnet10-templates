using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ProjectApp.IntegrationTests;

/// <summary>
/// Oturum iptali (security stamp), hesap kilitleme, kullanıcı uçları, stok eşzamanlılığı ve korelasyon kimliği.
/// </summary>
public class SecurityEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    // Kilit eşiği koddan okunur; MaxFailedAccessAttempts değişince test kırılmaz.
    private int MaxFailedAttempts =>
        factory.Services.GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout.MaxFailedAccessAttempts;

    private const string Api = "/api";

    [Fact]
    public async Task Logout_SonrasiEskiAccessToken_401Doner()
    {
        using var client = factory.CreateClient();
        var (_, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        client.Authorize(tokens.AccessToken);

        var logout = await client.PostAsync($"{Api}/auth/logout", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var me = await client.GetAsync($"{Api}/users/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task SifreDegisikligi_SonrasiEskiToken401_YeniSifreyleGirisCalisir()
    {
        using var client = factory.CreateClient();
        var (email, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        client.Authorize(tokens.AccessToken);
        const string newPassword = "YeniSifre12*";

        var change = await client.PostAsJsonAsync($"{Api}/users/change-password",
            new { currentPassword = TestHelpers.Password, newPassword }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var me = await client.GetAsync($"{Api}/users/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);

        var login = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = newPassword }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_BesYanlisDenemedenSonra_DogruSifreDe401Doner()
    {
        using var client = factory.CreateClient();
        var (email, _) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);

        for (var i = 0; i < MaxFailedAttempts; i++)
        {
            var wrong = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = "Yanlis12*Sifre" }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        var correct = await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = TestHelpers.Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
    }

    [Fact]
    public async Task ProfilGuncelleme_204DonerVeProfildeGorunur()
    {
        using var client = factory.CreateClient();
        var (_, tokens) = await TestHelpers.RegisterAndLoginAsync(factory, client, Api);
        client.Authorize(tokens.AccessToken);

        var me = await client.GetFromJsonAsync<ApiResponse<ProfileData>>($"{Api}/users/me", TestContext.Current.CancellationToken);
        var update = await client.PutAsJsonAsync($"{Api}/users/profile",
            new { fullName = "Güncel Ad", userName = me!.Data!.UserName, phoneNumber = "5551112233" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var after = await client.GetFromJsonAsync<ApiResponse<ProfileData>>($"{Api}/users/me", TestContext.Current.CancellationToken);
        Assert.Equal("Güncel Ad", after!.Data!.FullName);
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

    private sealed record ProfileData(Guid Id, string FullName, string UserName, string Email);

    private static async Task<Guid> CreateCategoryAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync($"{Api}/categories", new { name = $"Kat{Guid.NewGuid():N}"[..20] }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.ReadDataAsync<IdResponse>()).Id;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, decimal price)
    {
        var categoryId = await CreateCategoryAsync(client);
        var response = await client.PostAsJsonAsync($"{Api}/products",
            new { name = $"Urun {Guid.NewGuid():N}"[..20], price, stock = 10, categoryId }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.ReadDataAsync<IdResponse>()).Id;
    }
}
