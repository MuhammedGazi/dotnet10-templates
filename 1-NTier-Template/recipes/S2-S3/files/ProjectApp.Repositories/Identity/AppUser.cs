using Microsoft.AspNetCore.Identity;

namespace ProjectApp.Repositories.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
