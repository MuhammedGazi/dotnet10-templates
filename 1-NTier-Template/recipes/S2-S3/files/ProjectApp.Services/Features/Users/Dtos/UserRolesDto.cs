namespace ProjectApp.Services.Features.Users.Dtos;

public record UserRolesDto(Guid UserId, List<string> Roles);
