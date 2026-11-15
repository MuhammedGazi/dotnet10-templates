namespace ProjectApp.Services.Features.Users.Dtos;

public record UserProfileDto(
    Guid Id,
    string FullName,
    string? UserName,
    string? Email,
    string? PhoneNumber,
    IList<string> Roles);
