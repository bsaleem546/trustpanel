using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrustPanel.Domain.Billing;

namespace TrustPanel.Infrastructure.Persistence.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public static readonly Guid StarterPlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ProPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AgencyPlanId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AgencyPlusPlanId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");
        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(64).IsRequired();
        builder.Property(p => p.MonthlyPrice).HasPrecision(10, 2);
        builder.Property(p => p.AnnualPrice).HasPrecision(10, 2);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.HasData(
            new Plan
            {
                Id = StarterPlanId,
                Code = PlanCodes.Starter,
                Name = "Starter",
                MonthlyPrice = 29m,
                AnnualPrice = 276m,
                WorkspaceLimit = 1,
                TestimonialLimit = 100,
                WidgetLimit = 2
            },
            new Plan
            {
                Id = ProPlanId,
                Code = PlanCodes.Pro,
                Name = "Pro",
                MonthlyPrice = 59m,
                AnnualPrice = 564m,
                WorkspaceLimit = 3,
                TestimonialLimit = -1,
                WidgetLimit = 10,
                HasVideoTestimonials = true,
                HasAiFeatures = true,
                HasImportSources = true,
                HasFullAnalytics = true
            },
            new Plan
            {
                Id = AgencyPlanId,
                Code = PlanCodes.Agency,
                Name = "Agency",
                MonthlyPrice = 119m,
                AnnualPrice = 1140m,
                WorkspaceLimit = 10,
                TestimonialLimit = -1,
                WidgetLimit = -1,
                HasVideoTestimonials = true,
                HasAiFeatures = true,
                HasImportSources = true,
                HasFullAnalytics = true,
                HasWhiteLabel = true,
                HasCustomDomain = true,
                HasTeamMembers = true,
                HasApiAccess = true,
                HasWebhooks = true,
                HasCustomEmailSender = true
            },
            new Plan
            {
                Id = AgencyPlusPlanId,
                Code = PlanCodes.AgencyPlus,
                Name = "Agency+",
                MonthlyPrice = 199m,
                AnnualPrice = 1908m,
                WorkspaceLimit = -1,
                TestimonialLimit = -1,
                WidgetLimit = -1,
                HasVideoTestimonials = true,
                HasAiFeatures = true,
                HasImportSources = true,
                HasFullAnalytics = true,
                HasWhiteLabel = true,
                HasCustomDomain = true,
                HasTeamMembers = true,
                HasApiAccess = true,
                HasWebhooks = true,
                HasCustomEmailSender = true,
                HasAdvancedAiInsights = true,
                HasPrioritySupport = true
            });
    }
}

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(64).IsRequired();
        builder.Property(s => s.StripeCustomerId).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(s => s.StripeSubscriptionId).IsUnique();
        builder.HasIndex(s => s.UserId);
        builder.HasOne(s => s.Plan).WithMany().HasForeignKey(s => s.PlanId);
    }
}
