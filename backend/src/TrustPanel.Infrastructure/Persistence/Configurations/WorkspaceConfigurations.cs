using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Teams;
using TrustPanel.Domain.Workspaces;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");
        builder.Property(w => w.Slug).HasMaxLength(64).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(128).IsRequired();
        builder.Property(w => w.CustomDomain).HasMaxLength(255);
        builder.HasIndex(w => w.Slug).IsUnique();
        builder.HasIndex(w => w.CustomDomain).IsUnique();
        builder.HasIndex(w => w.OwnerUserId);
        builder.OwnsOne(w => w.Branding, b => b.ToJson());
        builder.OwnsOne(w => w.EmailFrom, b => b.ToJson());
    }
}

public sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_members");
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.InvitedEmail).HasMaxLength(256);
        builder.Property(m => m.InvitationTokenHash).HasMaxLength(128);
        builder.HasIndex(m => new { m.WorkspaceId, m.UserId })
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");
        builder.HasIndex(m => m.InvitationTokenHash);
        builder.HasIndex(m => m.UserId);
    }
}
