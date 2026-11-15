namespace ProjectApp.Services.Features.Auth.Dtos;

public record TokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt);
