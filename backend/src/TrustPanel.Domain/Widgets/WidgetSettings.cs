namespace TrustPanel.Domain.Widgets;

public class WidgetSettings
{
    public string CardStyle { get; set; } = "rounded";
    public string PrimaryColor { get; set; } = "#7C6AF7";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#1A1A2E";
    public string FontSize { get; set; } = "medium";
    public string Animation { get; set; } = "fade";
    public bool DarkMode { get; set; }
    public bool ShowRating { get; set; } = true;
    public bool ShowAvatar { get; set; } = true;
    public bool ShowDate { get; set; } = true;
    public bool ShowSource { get; set; }
}
