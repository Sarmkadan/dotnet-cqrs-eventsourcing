// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Exception for CQRS infrastructure and command/query processing errors.
/// </summary>
public class CqrsException : Exception
{
    public string ErrorCode { get; }
    public string? CorrelationId { get; }
    public DateTime OccurredAt { get; }

    public CqrsException(string message, string errorCode = "CQRS_ERROR", string? correlationId = null)
        : base(message)
    {
        ErrorCode = errorCode;
        CorrelationId = correlationId;
        OccurredAt = DateTime.UtcNow;
    }

    public CqrsException(string message, string errorCode, Exception innerException, string? correlationId = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        CorrelationId = correlationId;
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Exception raised when an aggregate is not found in the repository.
/// </summary>
public class AggregateNotFoundException : CqrsException
{
    public AggregateNotFoundException(string aggregateId, string aggregateType)
        : base($"{aggregateType} with ID '{aggregateId}' was not found.", "AGGREGATE_NOT_FOUND")
    {
    }
}

/// <summary>
/// Exception raised on event stream inconsistencies or optimistic concurrency failures.
/// </summary>
public class EventStreamException : CqrsException
{
    public EventStreamException(string message, string? correlationId = null)
        : base(message, "EVENT_STREAM_ERROR", correlationId)
    {
    }

    public EventStreamException(string message, Exception innerException, string? correlationId = null)
        : base(message, "EVENT_STREAM_ERROR", innerException, correlationId)
    {
    }
}
