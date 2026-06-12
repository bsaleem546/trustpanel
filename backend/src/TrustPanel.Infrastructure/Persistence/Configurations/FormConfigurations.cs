using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Forms;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class CollectionFormConfiguration : IEntityTypeConfiguration<CollectionForm>
{
    public void Configure(EntityTypeBuilder<CollectionForm> builder)
    {
        builder.ToTable("collection_forms");
        builder.Property(f => f.Slug).HasMaxLength(64).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(128).IsRequired();
        builder.Property(f => f.AllowedSubmissionType).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(f => new { f.WorkspaceId, f.Slug }).IsUnique();
        builder.OwnsOne(f => f.QuestionConfig, b => b.ToJson());
        builder.OwnsOne(f => f.ThankYouConfig, b => b.ToJson());
        builder.OwnsOne(f => f.RewardConfig, b => b.ToJson());
    }
}
