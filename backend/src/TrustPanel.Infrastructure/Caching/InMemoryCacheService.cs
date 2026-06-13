using System.Collections.Concurrent;
using System.Text.Json;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.Caching;

public sealed class InMemoryCacheService : ICacheService
{
    private sealed record Entry(string Json, DateTimeOffset ExpiresAt);
    private readonly ConcurrentDictionary<string, Entry> _cache = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
            return Task.FromResult(JsonSerializer.Deserialize<T>(entry.Json));
        _cache.TryRemove(key, out _);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken) where T : class
    {
        _cache[key] = new Entry(JsonSerializer.Serialize(value), DateTimeOffset.UtcNow.Add(expiry));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
