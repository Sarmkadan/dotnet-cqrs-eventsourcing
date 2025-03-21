// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware;

/// <summary>
/// Middleware that extracts and stores request context information.
/// Sets up correlation ID, request ID, user context for distributed tracing.
/// Makes context available throughout the request pipeline via AsyncLocal storage.
/// Enables correlating logs and events across multiple services in a distributed system.
/// </summary>
public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextMiddleware> _logger;

    public RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ExtractOrCreateCorrelationId(context);
        var requestId = Guid.NewGuid().ToString("N");

        // Store in both HttpContext.Items (for current request) and AsyncLocal (for async context)
        context.Items["CorrelationId"] = correlationId;
        context.Items["RequestId"] = requestId;
        context.Items["UserId"] = ExtractUserId(context);

        // Add correlation ID to response headers for client correlation
        context.Response.Headers.Add("X-Correlation-ID", correlationId);
        context.Response.Headers.Add("X-Request-ID", requestId);

        RequestContext.SetContext(new RequestContextInfo
        {
            CorrelationId = correlationId,
            RequestId = requestId,
            UserId = context.Items["UserId"]?.ToString(),
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            Method = context.Request.Method
        });

        _logger.LogInformation(
            "Request started: {Method} {Path} [CorrelationId: {CorrelationId}]",
            context.Request.Method,
            context.Request.Path,
            correlationId
        );

        try
        {
            await _next(context);
        }
        finally
        {
            _logger.LogInformation(
                "Request completed: {Method} {Path} -> {StatusCode} [CorrelationId: {CorrelationId}]",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                correlationId
            );

            RequestContext.Clear();
        }
    }

    /// <summary>
    /// Extracts or creates a correlation ID for tracing requests.
    /// Checks for X-Correlation-ID header; creates new GUID if not present.
    /// </summary>
    private static string ExtractOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            return correlationId.ToString();
        }

        if (context.Request.Headers.TryGetValue("X-Request-ID", out var requestId))
        {
            return requestId.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Extracts user ID from request (from header or claims).
    /// Returns null if no user information available.
    /// </summary>
    private static string? ExtractUserId(HttpContext context)
    {
        // Check for user ID in header
        if (context.Request.Headers.TryGetValue("X-User-ID", out var userId))
        {
            return userId.ToString();
        }

        // Check for authenticated user
        return context.User?.FindFirst("sub")?.Value;
    }
}

/// <summary>
/// Request context stored in AsyncLocal for access throughout async call stack.
/// Makes context available to domain logic, handlers, and services without passing through parameters.
/// </summary>
public class RequestContextInfo
{
    public string CorrelationId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}

/// <summary>
/// Static accessor for request context throughout the application.
/// Uses AsyncLocal to maintain context across async operations.
/// </summary>
public static class RequestContext
{
    private static readonly AsyncLocal<RequestContextInfo> Context = new();

    public static void SetContext(RequestContextInfo contextInfo)
    {
        Context.Value = contextInfo ?? throw new ArgumentNullException(nameof(contextInfo));
    }

    public static RequestContextInfo? GetContext()
    {
        return Context.Value;
    }

    public static string GetCorrelationId()
    {
        return Context.Value?.CorrelationId ?? Guid.NewGuid().ToString("N");
    }

    public static string GetRequestId()
    {
        return Context.Value?.RequestId ?? Guid.NewGuid().ToString("N");
    }

    public static string? GetUserId()
    {
        return Context.Value?.UserId;
    }

    public static void Clear()
    {
        Context.Value = null!;
    }
}

/// <summary>
/// Extension method to register request context middleware.
/// Place this early in the middleware pipeline to set up context for all downstream middleware.
/// </summary>
public static class RequestContextMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestContextMiddleware>();
    }
}
