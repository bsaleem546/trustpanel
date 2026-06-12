using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Users;
using TrustPanel.Infrastructure.Identity;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.StripeCustomerId).HasMaxLength(64);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(u => u.OnboardingState).HasColumnType("jsonb");
        builder.HasIndex(u => u.StripeCustomerId);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.UserAgent).HasMaxLength(512);
        builder.Property(t => t.IpAddress).HasMaxLength(64);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.SessionId);
    }
}

public sealed class SuperAdminOverrideConfiguration : IEntityTypeConfiguration<SuperAdminOverride>
{
    public void Configure(EntityTypeBuilder<SuperAdminOverride> builder)
    {
        builder.ToTable("super_admin_overrides");
        builder.Property(o => o.Reason).HasMaxLength(512);
        builder.HasIndex(o => o.UserId);
    }
}
