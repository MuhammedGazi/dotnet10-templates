using ProjectApp.Application.Common.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectApp.Infrastructure.Identity;

namespace ProjectApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Varsayılan roller (Admin, User) migration ile her ortama gelir; böylece Production'da
/// <c>dotnet ef database update</c> sonrası kayıt (register) rol bulamadığı için başarısız olmaz.
/// Id ve ConcurrencyStamp sabittir: değişirse EF her migration'da veri güncellemesi üretir.
/// </summary>
public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public static readonly Guid AdminRoleId = new("0193a1b2-0000-7000-8000-000000000001");
    public static readonly Guid UserRoleId = new("0193a1b2-0000-7000-8000-000000000002");

    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.HasData(
            new AppRole
            {
                Id = AdminRoleId,
                Name = RoleNames.Admin,
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "b6a1c9f0-0000-4000-8000-000000000001"
            },
            new AppRole
            {
                Id = UserRoleId,
                Name = RoleNames.User,
                NormalizedName = "USER",
                ConcurrencyStamp = "b6a1c9f0-0000-4000-8000-000000000002"
            });
    }
}
