using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Billing;

public class Subscription : Entity
{
    public Guid UserId { get; set; }
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string StripeCustomerId { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTimeOffset CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTimeOffset? GracePeriodEndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Plan? Plan { get; set; }
}
