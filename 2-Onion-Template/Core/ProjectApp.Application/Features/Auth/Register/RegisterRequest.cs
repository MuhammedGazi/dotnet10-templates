namespace ProjectApp.Application.Features.Auth.Register;

public record RegisterRequest(string FullName, string UserName, string Email, string Password);

public record RegisterResponse(Guid Id);
