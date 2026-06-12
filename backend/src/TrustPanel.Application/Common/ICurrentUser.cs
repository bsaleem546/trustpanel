namespace TrustPanel.Application.Common;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    bool IsSuperAdmin { get; }
    Guid? ImpersonatedByUserId { get; }
}
