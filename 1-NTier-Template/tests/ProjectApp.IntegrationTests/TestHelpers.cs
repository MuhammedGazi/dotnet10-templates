using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ProjectApp.IntegrationTests;

public record ApiResponse<T>(T? Data, List<string>? ErrorMessage);
public record IdResponse(Guid Id);
public record TokenData(string AccessToken, DateTime AccessTokenExpiresAt);
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

    public static void Authorize(this HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
}
