namespace ProjectApp.Application.Features.Auth.Dtos;

public record RefreshTokenRequest(string AccessToken, string RefreshToken);
