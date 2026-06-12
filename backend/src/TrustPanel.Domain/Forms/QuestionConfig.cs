namespace TrustPanel.Domain.Forms;

public class QuestionConfig
{
    public string WelcomeTitle { get; set; } = "Share your experience";
    public string WelcomeMessage { get; set; } = "We would love to hear your feedback.";
    public string Prompt { get; set; } = "What did you like most about working with us?";
    public bool CollectName { get; set; } = true;
    public bool CollectEmail { get; set; } = true;
    public bool CollectCompany { get; set; }
    public bool CollectJobTitle { get; set; }
    public bool CollectAvatar { get; set; }
    public bool CollectRating { get; set; } = true;
    public bool RequireEmail { get; set; } = true;
}
