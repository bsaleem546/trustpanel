namespace TrustPanel.Application.Common;

/// <summary>Server-side Cloudflare Turnstile token verification for public submissions.</summary>
public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken);
}
