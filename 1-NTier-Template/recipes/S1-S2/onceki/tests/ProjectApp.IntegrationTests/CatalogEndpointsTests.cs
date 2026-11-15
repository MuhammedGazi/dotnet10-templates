using System.Net;
using System.Net.Http.Json;

namespace ProjectApp.IntegrationTests;

public class CatalogEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Api = "/api";

    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = factory.CreateClient();
        client.Authorize(await factory.GetAdminAccessTokenAsync(client, Api));
        return client;
    }

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync($"{Api}/categories", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadDataAsync<IdResponse>()).Id;
    }

    [Fact]
    public async Task GetCategories_Anonim_200Doner()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"{Api}/categories", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_TokenYoksa_401Doner()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{Api}/categories", new { name = "Kategori" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_UserRoluyle_403Doner()
    {
        using var client = factory.CreateClient();
        var (_, tokens) = await factory.CreateUserAndLoginAsync(client, Api);
        client.Authorize(tokens.AccessToken);

        var response = await client.PostAsJsonAsync($"{Api}/categories", new { name = $"Kategori-{Guid.NewGuid():N}"[..20] }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Category_SilindiktenSonraAyniIsimleYenidenOlusturulabilir()
    {
        using var client = await CreateAuthorizedClientAsync();
        var name = $"Kategori-{Guid.NewGuid():N}"[..20];

        var id = await CreateCategoryAsync(client, name);
        var delete = await client.DeleteAsync($"{Api}/categories/{id}", TestContext.Current.CancellationToken);
        var recreate = await client.PostAsJsonAsync($"{Api}/categories", new { name }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Created, recreate.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_UrunuVarsa_409Doner()
    {
        using var client = await CreateAuthorizedClientAsync();
        var categoryId = await CreateCategoryAsync(client, $"Kategori-{Guid.NewGuid():N}"[..20]);
        var product = await client.PostAsJsonAsync($"{Api}/products",
            new { name = $"Urun-{Guid.NewGuid():N}"[..15], price = 10m, stock = 10, categoryId }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, product.StatusCode);

        var delete = await client.DeleteAsync($"{Api}/categories/{categoryId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task GetPagedProducts_SayfaBoyutunaGoreDoner()
    {
        using var client = await CreateAuthorizedClientAsync();
        var categoryId = await CreateCategoryAsync(client, $"Kategori-{Guid.NewGuid():N}"[..20]);
        for (var i = 0; i < 3; i++)
        {
            var created = await client.PostAsJsonAsync($"{Api}/products",
                new { name = $"Urun-{Guid.NewGuid():N}"[..15], price = 10m + i, stock = 10, categoryId }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        var response = await client.GetAsync($"{Api}/products/paged?pageNumber=1&pageSize=2", TestContext.Current.CancellationToken);
        var page = await response.ReadDataAsync<PagedData<ProductItem>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.TotalCount >= 3);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(2, page.PageSize);
    }

    [Fact]
    public async Task GetPagedProducts_CokBuyukSayfaNumarasi_BosSayfaDoner()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"{Api}/products/paged?pageNumber={int.MaxValue}&pageSize=100", TestContext.Current.CancellationToken);
        var page = await response.ReadDataAsync<PagedData<ProductItem>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task CreateCategory_LocationKokGoreliVeCalisir()
    {
        using var client = await CreateAuthorizedClientAsync();

        var response = await client.PostAsJsonAsync($"{Api}/categories", new { name = $"Kategori-{Guid.NewGuid():N}"[..20] }, TestContext.Current.CancellationToken);
        var location = response.Headers.Location;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(location);
        Assert.StartsWith("/", location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(location, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task HataYaniti_DataAlaniIcermez()
    {
        using var client = await CreateAuthorizedClientAsync();
        var name = $"Kategori-{Guid.NewGuid():N}"[..20];
        await CreateCategoryAsync(client, name);

        var duplicate = await client.PostAsJsonAsync($"{Api}/categories", new { name }, TestContext.Current.CancellationToken);
        using var json = System.Text.Json.JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.False(json.RootElement.TryGetProperty("data", out _));
        Assert.True(json.RootElement.TryGetProperty("errorMessage", out _));
    }

    [Fact]
    public async Task BozukJson_GenelMesajDonerVeTipAdiSizdirmaz()
    {
        using var client = await CreateAuthorizedClientAsync();

        using var content = new StringContent("{ \"name\": 123, \"price\": \"abc\" ", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{Api}/products", content, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("ProjectApp.", body, StringComparison.Ordinal);
    }
}
