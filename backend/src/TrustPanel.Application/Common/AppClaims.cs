namespace TrustPanel.Application.Common;

/// <summary>Custom JWT claim types used across the API.</summary>
public static class AppClaims
{
    public const string WorkspaceId = "workspace_id";
    public const string SessionId = "session_id";
    public const string ImpersonatedBy = "impersonated_by";
}
