using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using TrustPanel.Application.Common;
using TrustPanel.Application.PublicApi;


namespace TrustPanel.Api.Security;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions { }

public sealed class ApiKeyAuthenticationHandler
    : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IAppDbContext _db;
    private readonly IRateLimiter _rateLimiter;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger, UrlEncoder encoder,
        IAppDbContext db, IRateLimiter rateLimiter)
        : base(options, logger, encoder)
    {
        _db = db;
        _rateLimiter = rateLimiter;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var header = authHeader.ToString();
        if (!header.StartsWith("Bearer tp_live_", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var plaintext = header["Bearer ".Length..].Trim();
        var hash = CreateApiKeyCommandHandler.HashKey(plaintext);

        var key = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == hash && k.RevokedAt == null);

        if (key is null)
            return AuthenticateResult.Fail("Invalid API key.");

        // Rate limit: 1000/h per key.
        var allowed = await _rateLimiter.TryConsumeAsync(
            $"apikey:{key.Id}", 1000, TimeSpan.FromHours(1), CancellationToken.None);
        if (!allowed)
            return AuthenticateResult.Fail("Rate limit exceeded.");

        // Update LastUsedAt asynchronously (fire-and-forget update after response).
        key.LastUsedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(CancellationToken.None);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
            new Claim(AppClaims.WorkspaceId, key.WorkspaceId.ToString()),
            new Claim("api_key_id", key.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}

