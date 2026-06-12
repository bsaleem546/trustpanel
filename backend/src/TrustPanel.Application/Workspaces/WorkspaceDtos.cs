using TrustPanel.Domain.Workspaces;

namespace TrustPanel.Application.Workspaces;

public sealed record BrandingDto(
    string? LogoPath,
    string PrimaryColor,
    string SecondaryColor,
    string FontFamily,
    bool ShowTrustPanelBranding);

public sealed record EmailSenderDto(string? FromName, string? FromEmail);

public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string Slug,
    string? CustomDomain,
    DateTimeOffset? DomainVerifiedAt,
    BrandingDto Branding,
    EmailSenderDto EmailFrom,
    bool IsOwner,
    DateTimeOffset CreatedAt)
{
    public static WorkspaceDto From(Workspace workspace, Guid currentUserId) => new(
        workspace.Id,
        workspace.Name,
        workspace.Slug,
        workspace.CustomDomain,
        workspace.DomainVerifiedAt,
        new BrandingDto(
            workspace.Branding.LogoPath,
            workspace.Branding.PrimaryColor,
            workspace.Branding.SecondaryColor,
            workspace.Branding.FontFamily,
            workspace.Branding.ShowTrustPanelBranding),
        new EmailSenderDto(workspace.EmailFrom.FromName, workspace.EmailFrom.FromEmail),
        workspace.OwnerUserId == currentUserId,
        workspace.CreatedAt);
}
