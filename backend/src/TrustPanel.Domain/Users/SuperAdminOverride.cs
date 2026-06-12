using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Users;

public class SuperAdminOverride : Entity
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
