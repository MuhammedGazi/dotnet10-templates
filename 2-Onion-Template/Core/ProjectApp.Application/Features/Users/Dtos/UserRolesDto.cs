namespace ProjectApp.Application.Features.Users.Dtos;

public record UserRolesDto(Guid UserId, List<string> Roles);
