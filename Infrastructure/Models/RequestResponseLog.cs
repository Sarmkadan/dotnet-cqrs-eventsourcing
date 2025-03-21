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
    public string RequestId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
    public string? UserId { get; set; }
    public string ClientIp { get; set; } = string.Empty;
}

public class ResponseLog
{
    public string RequestId { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class ApiAuditLog
{
    public RequestLog Request { get; set; } = new();
    public ResponseLog Response { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool Success => Response.StatusCode >= 200 && Response.StatusCode < 300;
}

/// <summary>
/// Models for structured logging of operations.
/// </summary>
public class OperationLog
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString("N");
    public string OperationType { get; set; } = string.Empty; // e.g., "CreateAccount", "ProcessWithdraw"
    public string AggregateId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs => CompletedAt.HasValue ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds : 0;
    public OperationStatus Status { get; set; } = OperationStatus.InProgress;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();

    public void MarkSuccess()
    {
        Status = OperationStatus.Success;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailure(string errorMessage)
    {
        Status = OperationStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}

public enum OperationStatus
{
    InProgress,
    Success,
    Failed,
    Cancelled
}

/// <summary>
/// Models for event stream audit logging.
/// </summary>
public class EventAuditLog
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public int AggregateVersion { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Dictionary<string, object> EventData { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Exception details for structured error logging.
/// </summary>
public class ErrorLog
{
    public string ErrorId { get; set; } = Guid.NewGuid().ToString("N");
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }

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
