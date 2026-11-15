namespace ProjectApp.Services.Features.Auth.Dtos;

public record ResetPasswordRequest(string Email, string Token, string NewPassword);
