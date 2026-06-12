using System.Security.Claims;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Users;

namespace TrustPanel.Api.Security;

/// <summary>Resolves the current user from the authenticated HTTP context claims.</summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsSuperAdmin => Principal?.IsInRole(nameof(UserRole.SuperAdmin)) == true;

    public Guid? ImpersonatedByUserId =>
        Guid.TryParse(Principal?.FindFirstValue(AppClaims.ImpersonatedBy), out var id) ? id : null;
}
