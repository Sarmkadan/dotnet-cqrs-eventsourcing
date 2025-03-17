// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.Text;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware;

/// <summary>
/// HTTP middleware that logs incoming requests and outgoing responses with timing information.
/// Captures request body (if reasonable size), response status, and total elapsed time.
/// Useful for debugging, performance monitoring, and audit trails.
/// Skips logging for health check endpoints and static files to reduce noise.
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;
    private const int MaxBodySize = 10000; // Don't log request bodies larger than 10KB

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health checks and static files to reduce log noise
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        // Reset stream for downstream middleware to read
        context.Request.Body.Position = 0;

        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Read response body (being careful not to disrupt the original stream)
            var responseBody = responseBodyStream.ToArray();
            await originalResponseBody.WriteAsync(responseBody);
            context.Response.Body = originalResponseBody;

            LogRequest(context, stopwatch.ElapsedMilliseconds, requestBody, responseBody);
        }
    }

    /// <summary>
    /// Safely reads the request body without disrupting downstream middleware.
    /// Respects max size limit to avoid loading huge request payloads into memory.
    /// </summary>
    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.Body.CanSeek)
        {
            var buffer = new byte[Math.Min(request.ContentLength ?? 0, MaxBodySize)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
        }

        return "[Stream not seekable]";
    }

    /// <summary>
    /// Logs the complete request/response cycle with timing.
    /// Includes HTTP method, path, status code, and elapsed milliseconds for performance analysis.
    /// </summary>
    private void LogRequest(
        HttpContext context,
        long elapsedMs,
        string requestBody,
        byte[] responseBody)
    {
        var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error : LogLevel.Information;

        _logger.Log(
            logLevel,
            "HTTP {Method} {Path} -> {StatusCode} ({ElapsedMs}ms) | Request: {RequestBody}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsedMs,
            string.IsNullOrWhiteSpace(requestBody) ? "[empty]" : requestBody[..Math.Min(100, requestBody.Length)]
        );
    }

    /// <summary>
    /// Determines if this request should skip logging to reduce noise.
    /// Health checks, metrics, and static files are typically not worth logging.
    /// </summary>
    private static bool ShouldSkipLogging(PathString path)
    {
        var pathStr = path.Value?.ToLower() ?? string.Empty;
        return pathStr.Contains("/health") ||
               pathStr.Contains("/metrics") ||
               pathStr.Contains("/swagger") ||
               pathStr.StartsWith("/wwwroot") ||
               pathStr.EndsWith(".js") ||
               pathStr.EndsWith(".css");
    }
}

/// <summary>
/// Extension method to register logging middleware in the request pipeline.
/// Place this early in the middleware chain to capture complete request/response data.
/// </summary>
public static class LoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoggingMiddleware>();
    }
}
