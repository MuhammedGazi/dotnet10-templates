namespace ProjectApp.Services.Features.Users.Dtos;

/// <summary>Kullanıcının rollerini verilen listeyle değiştirir (yalnızca Admin).</summary>
public record UpdateUserRolesRequest(List<string> Roles);
