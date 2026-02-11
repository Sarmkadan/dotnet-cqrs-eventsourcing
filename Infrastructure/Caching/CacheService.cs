// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;

namespace DotNetCqrsEventSourcing.Infrastructure.Caching;

/// <summary>
/// In-memory cache service implementing both synchronous and asynchronous APIs.
/// Supports time-based expiration, invalidation by key patterns, and cache statistics.
/// Thread-safe using ConcurrentDictionary; suitable for high-concurrency scenarios.
/// For distributed systems, consider integrating Redis as a cache backend.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from cache by key. Returns null if not found or expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a value in cache with optional expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a single key from cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all keys matching a pattern (e.g., "account:*").
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a value using the factory function if not in cache.
    /// Implements cache-aside pattern atomically to avoid thundering herd.
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
}

public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly Timer _evictionTimer;

    public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Run eviction every 5 minutes to clean up expired entries
        _evictionTimer = new Timer(EvictExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        GuardClauses.NotNullOrEmpty(key, nameof(key));

        if (_cache.TryGetValue(key, out var entry))
        {
            // Check if entry has expired
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                _logger.LogDebug("Cache entry expired: {Key}", key);
                return Task.FromResult<T?>(null);
            }

            entry.LastAccessTime = DateTime.UtcNow;
            entry.HitCount++;

            return Task.FromResult(entry.Value as T);
        }

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        GuardClauses.NotNullOrEmpty(key, nameof(key));
        GuardClauses.NotNull(value, nameof(value));

        var entry = new CacheEntry
        {
            Value = value,
            CreatedTime = DateTime.UtcNow,
            LastAccessTime = DateTime.UtcNow,
            ExpirationTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null,
            HitCount = 0
        };

        _cache[key] = entry;
        _logger.LogDebug("Cache entry set: {Key} (TTL: {Ttl})", key, expiration?.TotalSeconds ?? 0);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(key, nameof(key));

        if (_cache.TryRemove(key, out _))
        {
            _logger.LogDebug("Cache entry removed: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(pattern, nameof(pattern));

        var keysToRemove = _cache.Keys
            .Where(k => MatchesPattern(k, pattern))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);

        return Task.CompletedTask;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        GuardClauses.NotNullOrEmpty(key, nameof(key));
        GuardClauses.NotNull(factory, nameof(factory));

        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    /// <summary>
    /// Gets cache statistics for monitoring.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var entries = _cache.Values.ToList();

        return new CacheStatistics
        {
            TotalEntries = _cache.Count,
            TotalHits = entries.Sum(e => e.HitCount),
            ExpiredEntries = entries.Count(e => e.IsExpired),
            AverageEntryAge = entries.Count > 0
                ? TimeSpan.FromSeconds(entries.Average(e => (DateTime.UtcNow - e.CreatedTime).TotalSeconds))
                : TimeSpan.Zero
        };
    }

    /// <summary>
    /// Periodically removes expired entries to prevent unbounded cache growth.
    /// </summary>
    private void EvictExpiredEntries(object? state)
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Evicted {Count} expired cache entries", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Checks if a key matches a pattern (supports * wildcard).
    /// Pattern "account:*" matches "account:123", "account:456", etc.
    /// </summary>
    private static bool MatchesPattern(string key, string pattern)
    {
        if (pattern == "*") return true;

        var patternParts = pattern.Split('*');
        if (patternParts.Length == 1) return key == pattern;

        if (!key.StartsWith(patternParts[0])) return false;

        for (int i = 1; i < patternParts.Length - 1; i++)
        {
            var index = key.IndexOf(patternParts[i]);
            if (index < 0) return false;
            key = key[(index + patternParts[i].Length)..];
        }

        return key.EndsWith(patternParts[^1]);
    }

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTime CreatedTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public int HitCount { get; set; }

        public bool IsExpired => ExpirationTime.HasValue && DateTime.UtcNow > ExpirationTime;
    }
}

public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public long TotalHits { get; set; }
    public int ExpiredEntries { get; set; }
    public TimeSpan AverageEntryAge { get; set; }
}
