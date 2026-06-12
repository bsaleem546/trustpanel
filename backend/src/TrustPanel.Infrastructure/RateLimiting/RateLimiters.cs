using System.Collections.Concurrent;
using StackExchange.Redis;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.RateLimiting;

/// <summary>Fixed-window counter in Redis: INCR + EXPIRE on first hit.</summary>
public sealed class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimiter(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> TryConsumeAsync(
        string key, int limit, TimeSpan window, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var redisKey = $"ratelimit:{key}";
        var count = await db.StringIncrementAsync(redisKey);
        if (count == 1)
        {
            await db.KeyExpireAsync(redisKey, window);
        }

        return count <= limit;
    }
}

/// <summary>Single-node fallback when Redis is not configured.</summary>
public sealed class InMemoryRateLimiter : IRateLimiter
{
    private sealed class Window
    {
        public long Count;
        public DateTimeOffset ExpiresAt;
    }

    private readonly ConcurrentDictionary<string, Window> _windows = new();

    public Task<bool> TryConsumeAsync(
        string key, int limit, TimeSpan window, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = _windows.AddOrUpdate(
            key,
            _ => new Window { Count = 1, ExpiresAt = now.Add(window) },
            (_, existing) =>
            {
                if (existing.ExpiresAt <= now)
                {
                    return new Window { Count = 1, ExpiresAt = now.Add(window) };
                }

                Interlocked.Increment(ref existing.Count);
                return existing;
            });

        // Opportunistic cleanup so abandoned windows do not accumulate.
        if (_windows.Count > 10_000)
        {
            foreach (var stale in _windows.Where(w => w.Value.ExpiresAt <= now).Take(1000))
            {
                _windows.TryRemove(stale.Key, out _);
            }
        }

        return Task.FromResult(Interlocked.Read(ref entry.Count) <= limit);
    }
}
