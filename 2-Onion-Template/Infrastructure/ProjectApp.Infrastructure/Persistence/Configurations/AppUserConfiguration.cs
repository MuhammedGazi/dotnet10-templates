using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectApp.Infrastructure.Identity;

namespace ProjectApp.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(150);

        // Refresh token SHA-256 özeti (hex, 64 karakter) olarak saklanır
        builder.Property(x => x.RefreshToken).HasMaxLength(64);
        builder.HasIndex(x => x.RefreshToken);
    }
}
