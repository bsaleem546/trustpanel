using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Integrations;

public class ImportSource : Entity
{
    public Guid WorkspaceId { get; set; }
    public ImportProvider Provider { get; set; }
    public string? ExternalAccountId { get; set; }
    public string Config { get; set; } = "{}";
    public DateTimeOffset? LastSyncedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
