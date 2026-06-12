using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Workspaces;

public class Workspace : Entity
{
    public Guid OwnerUserId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public DateTimeOffset? DomainVerifiedAt { get; set; }
    public WorkspaceBranding Branding { get; set; } = new();
    public EmailSenderSettings EmailFrom { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
