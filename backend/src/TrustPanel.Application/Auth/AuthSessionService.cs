using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Users;

namespace TrustPanel.Application.Auth;

/// <summary>Issues, rotates, and revokes refresh-token-backed sessions.</summary>
public sealed class AuthSessionService
{
    private readonly IAppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthSessionService(IAppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthResultDto> IssueAsync(
        AuthUserDto user, string userAgent, string ipAddress, CancellationToken cancellationToken)
    {
        var workspaceId = await _db.Workspaces
            .Where(w => w.OwnerUserId == user.Id)
            .OrderBy(w => w.CreatedAt)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var sessionId = Guid.NewGuid();
        return await CreateTokensAsync(user, workspaceId, sessionId, userAgent, ipAddress, cancellationToken);
    }

    public async Task<AuthResultDto> RotateAsync(
        string rawRefreshToken,
        Func<Guid, Task<AuthUserDto?>> findUser,
        string userAgent,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashToken(rawRefreshToken);
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            throw new UnauthorizedAppException("The refresh token is invalid or expired.");
        }

        var user = await findUser(existing.UserId)
            ?? throw new UnauthorizedAppException("The refresh token is invalid or expired.");

        existing.RevokedAt = DateTimeOffset.UtcNow;

        var workspaceId = await _db.Workspaces
            .Where(w => w.OwnerUserId == user.Id)
            .OrderBy(w => w.CreatedAt)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await CreateTokensAsync(
            user, workspaceId, existing.SessionId, userAgent, ipAddress, cancellationToken);
    }

    public async Task RevokeByRawTokenAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashToken(rawRefreshToken);
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, cancellationToken);

        if (token is not null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.SessionId == sessionId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            throw new NotFoundException("No active session was found with that ID.");
        }

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SessionDto>> ListSessionsAsync(
        Guid userId, Guid? currentSessionId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return tokens
            .GroupBy(t => t.SessionId)
            .Select(g => g.OrderByDescending(t => t.CreatedAt).First())
            .Select(t => new SessionDto(
                t.SessionId, t.UserAgent, t.IpAddress, t.CreatedAt, t.ExpiresAt,
                t.SessionId == currentSessionId))
            .ToList();
    }

    private async Task<AuthResultDto> CreateTokensAsync(
        AuthUserDto user,
        Guid? workspaceId,
        Guid sessionId,
        string userAgent,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_tokenService.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(rawRefreshToken),
            SessionId = sessionId,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            ExpiresAt = expiresAt
        });
        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(
            user.Id, user.Email, user.Role, workspaceId, sessionId);

        return new AuthResultDto(accessToken, rawRefreshToken, expiresAt, sessionId, user, workspaceId);
    }
}
