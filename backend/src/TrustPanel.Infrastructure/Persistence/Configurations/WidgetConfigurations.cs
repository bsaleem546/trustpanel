using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Analytics;
using TrustPanel.Domain.Widgets;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class WidgetConfiguration : IEntityTypeConfiguration<Widget>
{
    public void Configure(EntityTypeBuilder<Widget> builder)
    {
        builder.ToTable("widgets");
        builder.Property(w => w.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(w => w.Name).HasMaxLength(128).IsRequired();
        builder.Property(w => w.SourceFilter).HasConversion<string>().HasMaxLength(32);
        builder.Property(w => w.CustomCss).HasMaxLength(16384);
        builder.HasIndex(w => w.WorkspaceId);
        builder.OwnsOne(w => w.Settings, b => b.ToJson());
    }
}

public sealed class WidgetEventConfiguration : IEntityTypeConfiguration<WidgetEvent>
{
    public void Configure(EntityTypeBuilder<WidgetEvent> builder)
    {
        builder.ToTable("widget_events");
        builder.Property(e => e.Event).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.Country).HasMaxLength(2);
        builder.Property(e => e.Device).HasMaxLength(32);
        builder.Property(e => e.Referrer).HasMaxLength(2048);
        builder.HasIndex(e => new { e.WidgetId, e.OccurredAt });
        builder.HasIndex(e => e.OccurredAt);
    }
}

public sealed class WidgetAnalyticsDailyConfiguration : IEntityTypeConfiguration<WidgetAnalyticsDaily>
{
    public void Configure(EntityTypeBuilder<WidgetAnalyticsDaily> builder)
    {
        builder.ToTable("widget_analytics_daily");
        builder.Property(a => a.TopCountry).HasMaxLength(2);
        builder.Property(a => a.TopDevice).HasMaxLength(32);
        builder.HasIndex(a => new { a.WidgetId, a.Date }).IsUnique();
        builder.HasIndex(a => new { a.WorkspaceId, a.Date });
    }
}
