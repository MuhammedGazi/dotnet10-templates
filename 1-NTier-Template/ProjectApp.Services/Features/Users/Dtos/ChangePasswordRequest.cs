namespace ProjectApp.Services.Features.Users.Dtos;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
