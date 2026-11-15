using Microsoft.AspNetCore.Identity;

namespace ProjectApp.Repositories.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
}
