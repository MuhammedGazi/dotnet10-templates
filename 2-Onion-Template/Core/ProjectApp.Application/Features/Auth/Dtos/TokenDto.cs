namespace ProjectApp.Application.Features.Auth.Dtos;

public record TokenDto(
    string AccessToken,
    DateTime AccessTokenExpiry,
    string RefreshToken,
    DateTime RefreshTokenExpiry);
