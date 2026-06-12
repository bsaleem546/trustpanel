namespace TrustPanel.Application.Common;

/// <summary>
/// Fixed-window rate limiting. Backed by Redis in production and an in-memory
/// store when Redis is not configured (tests, bare local runs).
/// </summary>
public interface IRateLimiter
{
    /// <summary>Consumes one token; returns false when the limit is exhausted for the window.</summary>
    Task<bool> TryConsumeAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken);
}
