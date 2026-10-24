#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
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

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ICacheService"/> with per-key locking,
/// optional expiration, and periodic eviction of expired entries.
/// </summary>
public class InMemoryCacheService : ICacheService, IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();
    private readonly ILogger<InMemoryCacheService> _logger;
    private readonly Timer _evictionTimer;
    private bool _disposed;

    private const int EvictionIntervalMinutes = 5;
    private const int SemaphoreInitialCount = 1;
    private const int SemaphoreMaxCount = 1;
    private const double DefaultTtlSeconds = 0;
    private const string WildcardPattern = "*";

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryCacheService"/> class and starts the eviction timer.
    /// </summary>
    /// <param name="logger">Logger used to record cache activity.</param>
    public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        // Run eviction every 5 minutes to clean up expired entries
        _evictionTimer = new Timer(EvictExpiredEntries, null, TimeSpan.FromMinutes(EvictionIntervalMinutes), TimeSpan.FromMinutes(EvictionIntervalMinutes));
    }

    /// <summary>
    /// Retrieves a cached value by key, returning <see langword="null"/> when it is missing or expired.
    /// </summary>
    /// <typeparam name="T">The reference type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The cached value, or <see langword="null"/> if no live entry exists.</returns>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        GuardClauses.NotNullOrEmpty(key, nameof(key));

        if (_cache.TryGetValue(key, out var entry))
        {
            // Check if entry has expired
            if (entry.IsExpired)
            {
                // Remove the specific expired entry object to prevent concurrent Set from being clobbered
                _cache.TryRemoveEntry(key, entry);
                _logger.LogDebug("Cache entry expired: {Key}", key);
                return Task.FromResult<T?>(null);
            }

            entry.LastAccessTime = DateTime.UtcNow;
            entry.HitCount++;

            return Task.FromResult(entry.Value as T);
        }

        return Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Stores a value under the specified key, replacing any existing entry.
    /// </summary>
    /// <typeparam name="T">The reference type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional lifetime of the entry; when <see langword="null"/> the entry does not expire.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
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
        _logger.LogDebug("Cache entry set: {Key} (TTL: {Ttl})", key, expiration?.TotalSeconds ?? DefaultTtlSeconds);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the entry with the specified key, if present.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        GuardClauses.NotNullOrEmpty(key, nameof(key));

        if (_cache.TryRemove(key, out _))
        {
            _logger.LogDebug("Cache entry removed: {Key}", key);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all entries whose keys match the specified wildcard pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to match keys against.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);
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

    /// <summary>
    /// Returns the cached value for the key, or creates it with the factory and caches the result when it is missing.
    /// </summary>
    /// <typeparam name="T">The reference type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Delegate that produces the value on a cache miss.</param>
    /// <param name="expiration">Optional lifetime of the created entry; when <see langword="null"/> the entry does not expire.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The cached or newly created value.</returns>
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        GuardClauses.NotNullOrEmpty(key, nameof(key));
        GuardClauses.NotNull(factory, nameof(factory));

        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        // Serialize factory execution per key so concurrent misses for the same key
        // run the factory only once (cache-aside without a thundering herd).
        var keyLock = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(SemaphoreInitialCount, SemaphoreMaxCount));
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            cached = await GetAsync<T>(key, cancellationToken);
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
        finally
        {
            keyLock.Release();
        }
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
        var expiredEntries = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .ToList();

        foreach (var kvp in expiredEntries)
        {
            _cache.TryRemove(kvp.Key, out _);
        }

        if (expiredEntries.Count > 0)
        {
            _logger.LogInformation("Evicted {Count} expired cache entries", expiredEntries.Count);
        }
    }

    /// <summary>
    /// Checks if a key matches a pattern (supports * wildcard).
    /// Pattern "account:*" matches "account:123", "account:456", etc.
    /// </summary>
    private static bool MatchesPattern(string key, string pattern)
    {
        if (pattern == WildcardPattern)
        {
            return true;
        }

        var patternParts = pattern.Split('*');
        if (patternParts.Length == 1)
        {
            return key == pattern;
        }

        if (!key.StartsWith(patternParts[0], StringComparison.Ordinal))
        {
            return false;
        }

        // Consume the prefix so the remaining segments cannot overlap with it.
        key = key[patternParts[0].Length..];

        for (int i = 1; i < patternParts.Length - 1; i++)
        {
            var index = key.IndexOf(patternParts[i], StringComparison.Ordinal);
            if (index < 0)
            {
                return false;
            }

            key = key[(index + patternParts[i].Length)..];
        }

        return key.EndsWith(patternParts[^1], StringComparison.Ordinal);
    }

    /// <summary>
    /// Releases the eviction timer and per-key locks used by this cache instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _evictionTimer.Dispose();

        foreach (var keyLock in _keyLocks.Values)
        {
            keyLock.Dispose();
        }

        _keyLocks.Clear();
        GC.SuppressFinalize(this);
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

/// <summary>
/// Point-in-time statistics describing the contents of a cache.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>Gets or sets the number of entries currently held in the cache.</summary>
    public int TotalEntries { get; set; }

    /// <summary>Gets or sets the total number of cache hits recorded across all entries.</summary>
    public long TotalHits { get; set; }

    /// <summary>Gets or sets the number of entries that have passed their expiration time.</summary>
    public int ExpiredEntries { get; set; }

    /// <summary>Gets or sets the average age of the cached entries.</summary>
    public TimeSpan AverageEntryAge { get; set; }
}
