// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Net;

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

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitOptions? options = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? RateLimitOptions.Default();

        // Clean up expired buckets every 5 minutes to prevent memory bloat
        _cleanupTimer = new Timer(CleanupExpiredBuckets, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        var bucket = _buckets.GetOrAdd(clientIp, _ => new TokenBucket(_options.TokensPerMinute, _options.TokensPerMinute));

        if (!bucket.AllowRequest())
        {
            _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Try again later." });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Periodically removes buckets for clients that haven't made requests in a while.
    /// Prevents unbounded memory growth in long-running applications.
    /// </summary>
    private void CleanupExpiredBuckets(object? state)
    {
        var expiredKeys = _buckets
            .Where(kvp => DateTime.UtcNow - kvp.Value.LastAccessTime > TimeSpan.FromHours(1))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _buckets.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired rate limit buckets", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Extracts the client's IP address, accounting for proxies (X-Forwarded-For header).
    /// In production, place this service behind a trusted reverse proxy to prevent spoofing.
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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

public class RateLimitOptions
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
        return builder.UseMiddleware<RateLimitingMiddleware>(options);
    }
}
