namespace ProjectApp.Application.Features.Users.Dtos;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
