namespace TrustPanel.Application.Auth;

public interface ITokenService
{
    AccessTokenDto CreateAccessToken(
        Guid userId, string email, string role, Guid? workspaceId, Guid sessionId, Guid? impersonatedBy = null);

    /// <summary>Cryptographically random opaque refresh token (raw, sent to client once).</summary>
    string GenerateRefreshToken();

    /// <summary>SHA-256 hex hash used to store and look up refresh tokens.</summary>
    string HashToken(string rawToken);

    int RefreshTokenDays { get; }
}
