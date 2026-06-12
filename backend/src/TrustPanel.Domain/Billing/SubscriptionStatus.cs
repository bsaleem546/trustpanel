namespace TrustPanel.Domain.Billing;

public enum SubscriptionStatus
{
    Incomplete = 0,
    IncompleteExpired = 1,
    Trialing = 2,
    Active = 3,
    PastDue = 4,
    Canceled = 5,
    Unpaid = 6,
    Paused = 7
}
