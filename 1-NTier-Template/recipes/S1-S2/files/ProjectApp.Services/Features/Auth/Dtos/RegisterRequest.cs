namespace ProjectApp.Services.Features.Auth.Dtos;

public record RegisterRequest(string FullName, string UserName, string Email, string Password);

public record RegisterResponse(Guid Id);
