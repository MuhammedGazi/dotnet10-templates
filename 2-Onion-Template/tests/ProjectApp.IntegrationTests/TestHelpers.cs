using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ProjectApp.IntegrationTests;

public record ApiResponse<T>(T? Data, List<string>? ErrorMessage);
public record IdResponse(Guid Id);
public record TokenData(string AccessToken, string RefreshToken);
public record CurrentUserData(Guid Id, List<string> Roles);
public record ProductItem(Guid Id, string Name, decimal Price, int Stock, Guid CategoryId);
public record PagedData<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize);

public static class TestHelpers
{
    public const string Password = "Password12*";

    public static async Task<T> ReadDataAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        return body!.Data!;
    }

    /// <summary>
    /// Kayıt, kapalı kayıt modunda (Registration:AllowPublic=false) da çalışsın diye admin token'ıyla yapılır.
    /// Token istek bazlı başlıkla gönderilir; client'ın varsayılan Authorization başlığı değişmez.
    /// </summary>
    public static async Task<(string Email, TokenData Tokens)> RegisterAndLoginAsync(CustomWebApplicationFactory factory, HttpClient client, string routePrefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"user-{suffix}@example.com";
        var adminToken = await factory.GetAdminAccessTokenAsync(client, routePrefix);

        var register = await PostRegisterAsync(client, routePrefix, adminToken,
            new { fullName = "Test Kullanıcı", userName = $"user{suffix}", email, password = Password });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync($"{routePrefix}/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        return (email, await login.ReadDataAsync<TokenData>());
    }

    /// <summary>
    /// Register isteğini verilen token ile (null ise anonim) gönderir; client başlıklarını değiştirmez.
    /// </summary>
    public static async Task<HttpResponseMessage> PostRegisterAsync(HttpClient client, string routePrefix, string? accessToken, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{routePrefix}/auth/register") { Content = JsonContent.Create(body) };
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    public static void Authorize(this HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}
