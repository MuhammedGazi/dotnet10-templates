namespace ProjectApp.Application.Features.Users.Dtos;

public record ChangeEmailRequest(
    string CurrentPassword,
    string NewEmail);
