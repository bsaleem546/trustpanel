using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Integrations;

public class WebhookEndpoint : Entity
{
    public Guid WorkspaceId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
