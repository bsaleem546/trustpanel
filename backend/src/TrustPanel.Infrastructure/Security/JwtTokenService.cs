using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TrustPanel.Application.Auth;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    /// <summary>Development-only fallback so local/test runs work without configuration.</summary>
    public const string DevelopmentSigningKey =
        "trustpanel-development-signing-key-do-not-use-in-production";

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SigningCredentials _signingCredentials;
    private readonly int _accessTokenMinutes;

    public int RefreshTokenDays { get; }

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["JWT_ISSUER"] ?? "trustpanel";
        _audience = configuration["JWT_AUDIENCE"] ?? "trustpanel-api";
        _accessTokenMinutes = int.TryParse(configuration["ACCESS_TOKEN_MINUTES"], out var minutes)
            ? minutes
            : 15;
        RefreshTokenDays = int.TryParse(configuration["REFRESH_TOKEN_DAYS"], out var days) ? days : 30;

        var signingKey = configuration["JWT_SIGNING_KEY"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            signingKey = DevelopmentSigningKey;
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenDto CreateAccessToken(
        Guid userId, string email, string role, Guid? workspaceId, Guid sessionId,
        Guid? impersonatedBy = null)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_accessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new(AppClaims.SessionId, sessionId.ToString())
        };

        if (workspaceId is not null)
        {
            claims.Add(new Claim(AppClaims.WorkspaceId, workspaceId.Value.ToString()));
        }

        if (impersonatedBy is not null)
        {
            claims.Add(new Claim(AppClaims.ImpersonatedBy, impersonatedBy.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _signingCredentials);

        return new AccessTokenDto(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
