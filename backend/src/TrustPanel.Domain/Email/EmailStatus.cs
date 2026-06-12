namespace TrustPanel.Domain.Email;

public enum EmailStatus
{
    Queued = 0,
    Sent = 1,
    Delivered = 2,
    Bounced = 3,
    Complained = 4,
    Failed = 5
}
