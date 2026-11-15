namespace ProjectApp.Application.Features.Users.Dtos;

public record ConfirmEmailChangeRequest(
    string NewEmail,
    string Token);
