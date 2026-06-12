using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Common;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.Property(a => a.Action).HasMaxLength(128).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.Metadata).HasColumnType("jsonb");
        builder.HasIndex(a => new { a.WorkspaceId, a.CreatedAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
