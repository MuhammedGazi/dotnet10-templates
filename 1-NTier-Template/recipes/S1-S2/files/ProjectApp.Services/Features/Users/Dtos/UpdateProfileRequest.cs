namespace ProjectApp.Services.Features.Users.Dtos;

public record UpdateProfileRequest(
    string FullName,
    string UserName,
    string? PhoneNumber);
