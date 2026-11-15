namespace ProjectApp.Services.Features.Users.Dtos;

public record ConfirmEmailChangeRequest(
    string NewEmail,
    string Token);
