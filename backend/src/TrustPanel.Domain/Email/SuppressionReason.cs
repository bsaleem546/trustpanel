namespace TrustPanel.Domain.Email;

public enum SuppressionReason
{
    Unsubscribed = 0,
    Bounced = 1,
    Complained = 2,
    Manual = 3
}
