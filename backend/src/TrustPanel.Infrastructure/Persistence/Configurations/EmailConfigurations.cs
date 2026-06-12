using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Email;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");
        builder.Property(e => e.Template).HasConversion<string>().HasMaxLength(48);
        builder.Property(e => e.Recipient).HasMaxLength(256).IsRequired();
        builder.Property(e => e.ProviderMessageId).HasMaxLength(128);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.Error).HasMaxLength(2048);
        builder.HasIndex(e => new { e.WorkspaceId, e.Recipient });
        builder.HasIndex(e => e.ProviderMessageId);
        builder.HasIndex(e => e.CreatedAt);
    }
}

public sealed class EmailSuppressionConfiguration : IEntityTypeConfiguration<EmailSuppression>
{
    public void Configure(EntityTypeBuilder<EmailSuppression> builder)
    {
        builder.ToTable("email_suppressions");
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Reason).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(e => new { e.WorkspaceId, e.Email }).IsUnique();
    }
}
