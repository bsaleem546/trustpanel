using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Forms;

public class CollectionForm : Entity
{
    public Guid WorkspaceId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SubmissionType AllowedSubmissionType { get; set; } = SubmissionType.Text;
    public QuestionConfig QuestionConfig { get; set; } = new();
    public ThankYouConfig ThankYouConfig { get; set; } = new();
    public RewardConfig RewardConfig { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
