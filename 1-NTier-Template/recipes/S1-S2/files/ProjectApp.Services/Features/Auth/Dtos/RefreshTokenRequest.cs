namespace ProjectApp.Services.Features.Auth.Dtos;

public record RefreshTokenRequest(string AccessToken, string RefreshToken);
