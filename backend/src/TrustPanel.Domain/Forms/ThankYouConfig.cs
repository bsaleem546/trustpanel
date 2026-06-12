namespace TrustPanel.Domain.Forms;

public class ThankYouConfig
{
    public string Title { get; set; } = "Thank you!";
    public string Message { get; set; } = "Your testimonial means a lot to us.";
    public string? RedirectUrl { get; set; }
    public bool ShowSocialShare { get; set; }
}
