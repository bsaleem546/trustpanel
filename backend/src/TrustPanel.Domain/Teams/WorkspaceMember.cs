using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Teams;

public class WorkspaceMember : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid? UserId { get; set; }
    public WorkspaceRole Role { get; set; } = WorkspaceRole.Viewer;
    public string? InvitedEmail { get; set; }
    public string? InvitationTokenHash { get; set; }
    public DateTimeOffset? InvitationExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
