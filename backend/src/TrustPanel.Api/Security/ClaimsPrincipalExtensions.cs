using System.Security.Claims;
using TrustPanel.Application.Common;

namespace TrustPanel.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAppException("Authentication is required.");
    }

    public static Guid? GetSessionId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(AppClaims.SessionId);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static Guid? GetWorkspaceId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(AppClaims.WorkspaceId);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
