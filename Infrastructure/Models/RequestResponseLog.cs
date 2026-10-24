#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Models;

/// <summary>
/// Request/response logging models for API audit trail and debugging.
/// Captures complete HTTP conversation for troubleshooting and compliance.
/// </summary>
public class RequestLog
{
    /// <summary>
    /// Unique identifier of the HTTP request.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Correlation identifier used to link related log entries across services.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Time at which the request was received, in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// HTTP method of the request (for example, GET or POST).
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Request path without the query string.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Raw query string of the request, or <see langword="null"/> when none was sent.
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// Request headers captured for the audit trail.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Request body, or <see langword="null"/> when the request has no body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Identifier of the authenticated user that made the request, if any.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// IP address of the client that sent the request.
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;
}

/// <summary>
/// Captures the HTTP response that was sent for a logged request.
/// </summary>
public class ResponseLog
{
    /// <summary>
    /// Identifier of the request this response belongs to.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Time taken to produce the response, in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Response body, or <see langword="null"/> when the response has no body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Response headers captured for the audit trail.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Time at which the response was produced, in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Pairs a logged request with its response and any error message.
/// </summary>
public class ApiAuditLog
{
    /// <summary>
    /// The logged request.
    /// </summary>
    public RequestLog Request { get; set; } = new();

    /// <summary>
    /// The logged response.
    /// </summary>
    public ResponseLog Response { get; set; } = new();

    /// <summary>
    /// Error message associated with the call, or <see langword="null"/> when there was none.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets a value indicating whether the response status code is in the 2xx range.
    /// </summary>
    public bool Success => Response.StatusCode >= 200 && Response.StatusCode < 300;
}

/// <summary>
/// Models for structured logging of operations.
/// </summary>
public class OperationLog
{
    /// <summary>
    /// Unique identifier of the operation.
    /// </summary>
    public string OperationId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Name of the operation type, for example "CreateAccount" or "ProcessWithdraw".
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the aggregate the operation acts on.
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Time at which the operation started, in UTC.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Time at which the operation completed, or <see langword="null"/> while it is still running.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Elapsed time between <see cref="StartedAt"/> and <see cref="CompletedAt"/> in milliseconds, or 0 when not completed.
    /// </summary>
    public long DurationMs => CompletedAt.HasValue ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds : 0;

    /// <summary>
    /// Current status of the operation.
    /// </summary>
    public OperationStatus Status { get; set; } = OperationStatus.InProgress;

    /// <summary>
    /// Error message recorded when the operation failed, or <see langword="null"/> otherwise.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional contextual data attached to the operation.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Marks the operation as successful and records its completion time.
    /// </summary>
    public void MarkSuccess()
    {
        Status = OperationStatus.Success;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the operation as failed, stores the error message and records its completion time.
    /// </summary>
    /// <param name="errorMessage">Description of the failure.</param>
    public void MarkFailure(string errorMessage)
    {
        Status = OperationStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Lifecycle status of a logged operation.
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// The operation is still running.
    /// </summary>
    InProgress,

    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The operation failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The operation was cancelled before completion.
    /// </summary>
    Cancelled
}

/// <summary>
/// Models for event stream audit logging.
/// </summary>
public class EventAuditLog
{
    /// <summary>
    /// Unique identifier of the recorded event.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Name of the event type that was recorded.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the aggregate that emitted the event.
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Version of the aggregate after the event was applied.
    /// </summary>
    public long AggregateVersion { get; set; }

    /// <summary>
    /// Time at which the event was recorded, in UTC.
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identifier of the user who caused the event, if known.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Correlation identifier linking the event to the originating request.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Payload of the event as key-value pairs.
    /// </summary>
    public Dictionary<string, object> EventData { get; set; } = new();

    /// <summary>
    /// Additional metadata stored alongside the event.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Exception details for structured error logging.
/// </summary>
public sealed class ErrorLog
{
    /// <summary>
    /// Unique identifier of the error entry.
    /// </summary>
    public string ErrorId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Name of the exception type.
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Message of the exception.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Stack trace of the exception, or <see langword="null"/> when unavailable.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Additional contextual data attached to the error.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Time at which the error was logged, in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation identifier linking the error to the originating request, if any.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Creates an <see cref="ErrorLog"/> entry from an exception.
    /// </summary>
    /// <param name="ex">The exception to convert.</param>
    /// <param name="correlationId">Optional correlation identifier to attach to the entry.</param>
    /// <returns>A new <see cref="ErrorLog"/> populated from the exception.</returns>
    public static ErrorLog FromException(Exception ex, string? correlationId = null)
    {
        return new ErrorLog
        {
            ErrorType = ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            CorrelationId = correlationId
        };
    }
}
