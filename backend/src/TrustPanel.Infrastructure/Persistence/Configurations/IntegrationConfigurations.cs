using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Integrations;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.Property(k => k.Name).HasMaxLength(128).IsRequired();
        builder.Property(k => k.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(k => k.Prefix).HasMaxLength(16).IsRequired();
        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasIndex(k => k.WorkspaceId);
        builder.Ignore(k => k.IsActive);
    }
}

public sealed class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<WebhookEndpoint> builder)
    {
        builder.ToTable("webhook_endpoints");
        builder.Property(w => w.Url).HasMaxLength(2048).IsRequired();
        builder.Property(w => w.Secret).HasMaxLength(128).IsRequired();
        builder.HasIndex(w => w.WorkspaceId);
    }
}

public sealed class ImportSourceConfiguration : IEntityTypeConfiguration<ImportSource>
{
    public void Configure(EntityTypeBuilder<ImportSource> builder)
    {
        builder.ToTable("import_sources");
        builder.Property(s => s.Provider).HasConversion<string>().HasMaxLength(32);
        builder.Property(s => s.ExternalAccountId).HasMaxLength(256);
        builder.Property(s => s.Config).HasColumnType("jsonb");
        builder.HasIndex(s => new { s.WorkspaceId, s.Provider });
    }
}
