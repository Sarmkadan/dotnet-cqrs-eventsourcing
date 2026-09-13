#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Net;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware;

/// <summary>
/// Token bucket rate limiting middleware that enforces per-IP request quotas.
/// Allows burst traffic while maintaining average throughput limits.
/// When rate limit exceeded, returns 429 Too Many Requests with Retry-After header.
/// Thread-safe using concurrent collections for production use under high concurrency.
/// Automatically cleans up expired buckets to prevent memory leaks.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly Timer _cleanupTimer;

    // Constants for magic numbers and strings
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private const string InitializationLogMessage = "Rate limiting middleware initialized with {TokensPerMinute} tokens per minute";
    private const string RetryAfterHeaderName = "Retry-After";
    private const string RetryAfterHeaderValue = "60";
    private const string RateLimitExceededMessage = "Rate limit exceeded. Try again later.";
    private static readonly TimeSpan BucketExpiration = TimeSpan.FromHours(1);
    private const string CleanupLogMessage = "Cleaned up {Count} expired rate limit buckets";
    private const string XForwardedForHeader = "X-Forwarded-For";
    private const string UnknownIpAddress = "unknown";
    private const string ProcessingRateLimitCheckLog = "Processing rate limit check for client {ClientIp}";
    private const string RateLimitExceededWarningLog = "Rate limit exceeded for IP: {ClientIp}";

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _logger = logger;
        _options = options ?? RateLimitOptions.Default();

        // Clean up expired buckets every 5 minutes to prevent memory bloat
        _cleanupTimer = new Timer(CleanupExpiredBuckets, null, CleanupInterval, CleanupInterval);

        _logger.LogInformation(InitializationLogMessage, _options.TokensPerMinute);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var clientIp = GetClientIpAddress(context);
        var bucket = _buckets.GetOrAdd(clientIp, _ => new TokenBucket(_options.TokensPerMinute, _options.TokensPerMinute));

        _logger.LogInformation(ProcessingRateLimitCheckLog, clientIp);

        if (!bucket.AllowRequest())
        {
            _logger.LogWarning(RateLimitExceededWarningLog, clientIp);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers[RetryAfterHeaderName] = RetryAfterHeaderValue;
            await context.Response.WriteAsJsonAsync(new { error = RateLimitExceededMessage });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Gets the current rate limit options used by this middleware instance.
    /// </summary>
    /// <returns>The rate limit configuration options.</returns>
    public RateLimitOptions GetRateLimitOptions() => _options;

    /// <summary>
    /// Gets the current state of all token buckets for monitoring and serialization purposes.
    /// </summary>
    /// <returns>A dictionary mapping client IPs to their token bucket states.</returns>
    public Dictionary<string, TokenBucket> GetBucketState() => new(_buckets);

    /// <summary>
    /// Periodically removes buckets for clients that haven't made requests in a while.
    /// Prevents unbounded memory growth in long-running applications.
    /// </summary>
    private void CleanupExpiredBuckets(object? state)
    {
        var expiredKeys = _buckets
            .Where(kvp => DateTime.UtcNow - kvp.Value.LastAccessTime > BucketExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _buckets.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation(CleanupLogMessage, expiredKeys.Count);
        }
    }

    /// <summary>
    /// Extracts the client's IP address, accounting for proxies (X-Forwarded-For header).
    /// In production, place this service behind a trusted reverse proxy to prevent spoofing.
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(XForwardedForHeader, out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownIpAddress;
    }

    /// <summary>
    /// Token bucket algorithm: tokens refill at a constant rate.
    /// Allows burst traffic up to the bucket size, then enforces average rate.
    /// Each request consumes one token; requests fail if bucket empty.
    /// </summary>
    private class TokenBucket
    {
        private double _tokens;
        private readonly double _maxTokens;
        private readonly double _tokensPerSecond;
        private DateTime _lastRefillTime;

        public DateTime LastAccessTime { get; private set; }

        public TokenBucket(double tokensPerMinute, double maxTokens)
        {
            _maxTokens = maxTokens;
            _tokensPerSecond = tokensPerMinute / 60.0;
            _tokens = maxTokens;
            _lastRefillTime = DateTime.UtcNow;
            LastAccessTime = DateTime.UtcNow;
        }

        public bool AllowRequest()
        {
            RefillTokens();
            LastAccessTime = DateTime.UtcNow;

            if (_tokens >= 1)
            {
                _tokens -= 1;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates how many tokens to add based on elapsed time since last refill.
        /// Caps at max tokens to prevent overflow.
        /// </summary>
        private void RefillTokens()
        {
            var now = DateTime.UtcNow;
            var timeElapsed = (now - _lastRefillTime).TotalSeconds;
            _tokens = Math.Min(_maxTokens, _tokens + timeElapsed * _tokensPerSecond);
            _lastRefillTime = now;
        }
    }
}

public sealed class RateLimitOptions
{
    public double TokensPerMinute { get; set; }
    public bool Enabled { get; set; }

    public static RateLimitOptions Default() => new() { TokensPerMinute = 60, Enabled = true };
    public static RateLimitOptions Disabled() => new() { Enabled = false };
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, RateLimitOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseMiddleware<RateLimitingMiddleware>(options);
    }
}
