#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Models;

/// <summary>
/// Extension methods for RequestLog to provide common operations and utilities.
/// </summary>
public static class RequestLogExtensions
{
    /// <summary>
    /// Determines if the request represents a read-only operation (GET, HEAD, OPTIONS).
    /// </summary>
    /// <param name="requestLog">The request log to check</param>
    /// <returns>True if the request is read-only; otherwise false</returns>
    public static bool IsReadOnlyOperation(this RequestLog requestLog)
    {
        ArgumentNullException.ThrowIfNull(requestLog);

        return requestLog.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
               requestLog.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) ||
               requestLog.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the request represents a write operation (POST, PUT, DELETE, PATCH).
    /// </summary>
    /// <param name="requestLog">The request log to check</param>
    /// <returns>True if the request is a write operation; otherwise false</returns>
    public static bool IsWriteOperation(this RequestLog requestLog)
    {
        ArgumentNullException.ThrowIfNull(requestLog);

        return !requestLog.IsReadOnlyOperation();
    }

    /// <summary>
    /// Gets the client IP address from the request, handling common proxy headers.
    /// </summary>
    /// <param name="requestLog">The request log</param>
    /// <returns>The resolved client IP address</returns>
    public static string GetClientIpAddress(this RequestLog requestLog)
    {
        ArgumentNullException.ThrowIfNull(requestLog);

        // If ClientIp is already populated, use it
        if (!string.IsNullOrWhiteSpace(requestLog.ClientIp))
        {
            return requestLog.ClientIp;
        }

        // Try to extract from headers if available
        if (requestLog.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) &&
            !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ips = forwardedFor.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return ips.FirstOrDefault()?.Trim() ?? string.Empty;
        }

        if (requestLog.Headers.TryGetValue("X-Real-IP", out var realIp) &&
            !string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.Trim();
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the user agent from the request headers.
    /// </summary>
    /// <param name="requestLog">The request log</param>
    /// <returns>The user agent string if present; otherwise null</returns>
    public static string? GetUserAgent(this RequestLog requestLog)
    {
        ArgumentNullException.ThrowIfNull(requestLog);

        requestLog.Headers.TryGetValue("User-Agent", out var userAgent);
        return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
    }

    /// <summary>
    /// Creates a correlation ID from the request if one is not already set.
    /// </summary>
    /// <param name="requestLog">The request log</param>
    /// <returns>The correlation ID (either existing or generated)</returns>
    public static string EnsureCorrelationId(this RequestLog requestLog)
    {
        ArgumentNullException.ThrowIfNull(requestLog);

        if (string.IsNullOrWhiteSpace(requestLog.CorrelationId))
        {
            requestLog.CorrelationId = Guid.NewGuid().ToString("N");
        }

        return requestLog.CorrelationId;
    }
}