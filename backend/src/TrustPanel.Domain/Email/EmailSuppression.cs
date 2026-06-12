using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Email;

public class EmailSuppression : Entity
{
    public Guid WorkspaceId { get; set; }
    public string Email { get; set; } = string.Empty;
    public SuppressionReason Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
