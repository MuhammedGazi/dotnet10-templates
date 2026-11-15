namespace ProjectApp.Services.Features.Users.Dtos;

public record ChangeEmailRequest(
    string CurrentPassword,
    string NewEmail);
