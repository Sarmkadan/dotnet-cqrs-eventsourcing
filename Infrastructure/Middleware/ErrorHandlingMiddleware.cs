// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using System.Text.Json;
using DotNetCqrsEventSourcing.Shared.Exceptions;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions and converts them to consistent HTTP responses.
/// Maps domain-specific exceptions to appropriate HTTP status codes:
/// - DomainException -> 400 BadRequest (business rule violations)
/// - CqrsException -> 400 BadRequest (command/query processing errors)
/// - General Exception -> 500 InternalServerError (unexpected errors)
/// This ensures API clients receive predictable error structures regardless of exception type.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Translates exceptions to HTTP responses and ensures consistent error message format.
    /// Domain exceptions are expected during normal operation; log them at warning level.
    /// Unexpected exceptions are logged at error level for investigation.
    /// </summary>
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var errorId = Guid.NewGuid().ToString("N")[..8];
        var (statusCode, message, details) = exception switch
        {
            // Business rule violations - client error, expected during normal operation
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                "Domain rule violation",
                new[] { domainEx.Message }
            ),

            // CQRS processing errors - validation, state inconsistency, etc.
            CqrsException cqrsEx => (
                HttpStatusCode.BadRequest,
                "Operation failed",
                new[] { cqrsEx.Message }
            ),

            // Argument validation errors - client passed invalid data
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                "Invalid argument",
                new[] { argEx.Message }
            ),

            // Unexpected errors - log for investigation
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred",
                new[] { exception.Message }
            )
        };

        // Log with appropriate severity
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception [ErrorId: {ErrorId}]", errorId);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception [ErrorId: {ErrorId}]: {Message}", errorId, message);
        }

        context.Response.StatusCode = (int)statusCode;

        var errorResponse = new ErrorResponse
        {
            ErrorId = errorId,
            Message = message,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(errorResponse, options);

        return context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Standard error response structure returned by all error scenarios.
    /// Includes a unique error ID for tracking, timestamp for correlation logs,
    /// and detailed error information for debugging.
    /// </summary>
    private class ErrorResponse
    {
        public string ErrorId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string[] Details { get; set; } = Array.Empty<string>();
        public DateTime Timestamp { get; set; }
    }
}

/// <summary>
/// Extension to register global error handling in the request pipeline.
/// Place this as the first middleware in the pipeline to catch exceptions from all downstream middleware.
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
