using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ProjectApp.Repositories.Identity;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(150);
        builder.Property(x => x.RefreshToken).HasMaxLength(64);
        builder.HasIndex(x => x.RefreshToken);
    }
}
