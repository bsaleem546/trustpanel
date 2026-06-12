using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> builder)
    {
        builder.ToTable("testimonials");
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(t => t.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.VideoPath).HasMaxLength(512);
        builder.Property(t => t.ThumbnailPath).HasMaxLength(512);
        builder.Property(t => t.Highlight).HasMaxLength(1024);
        builder.HasIndex(t => new { t.WorkspaceId, t.Status });
        builder.HasIndex(t => t.CollectionFormId);
        builder.HasIndex(t => t.CreatedAt);
        builder.OwnsOne(t => t.Submitter, b => b.ToJson());
    }
}
