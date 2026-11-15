namespace ProjectApp.Services.Features.Auth.Dtos;

public record CurrentUserResponse(
    Guid Id,
    string? UserName,
    string? Email,
    string? FullName,
    List<string> Roles);
