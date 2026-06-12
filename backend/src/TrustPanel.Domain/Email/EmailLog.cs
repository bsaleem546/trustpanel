using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Email;

public class EmailLog : Entity
{
    public Guid WorkspaceId { get; set; }
    public EmailTemplateType Template { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Queued;
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
