namespace TrustPanel.Domain.Workspaces;

public class WorkspaceBranding
{
    public string? LogoPath { get; set; }
    public string PrimaryColor { get; set; } = "#7C6AF7";
    public string SecondaryColor { get; set; } = "#1A1A2E";
    public string FontFamily { get; set; } = "Inter";
    public bool ShowTrustPanelBranding { get; set; } = true;
}
