#nullable enable
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
    /// <summary>
    /// Gets the machine-readable error code describing the failure.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the correlation identifier used to trace the failing operation, or <see langword="null"/> if none was supplied.
    /// </summary>
    public string? CorrelationId { get; internal set; }

    /// <summary>
    /// Gets the UTC date and time at which the exception was created.
    /// </summary>
    public DateTime OccurredAt { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CqrsException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The machine-readable error code. Defaults to <c>CQRS_ERROR</c>.</param>
    /// <param name="correlationId">The optional correlation identifier of the failing operation.</param>
    public CqrsException(string message, string errorCode = "CQRS_ERROR", string? correlationId = null)
        : base(message)
    {
        ErrorCode = errorCode;
        CorrelationId = correlationId;
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CqrsException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="innerException">The exception that caused the current exception, or <see langword="null"/>.</param>
    /// <param name="correlationId">The optional correlation identifier of the failing operation.</param>
    public CqrsException(string message, string errorCode, Exception? innerException, string? correlationId = null)
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
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateNotFoundException"/> class.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate that was not found.</param>
    /// <param name="aggregateType">The type name of the aggregate that was not found.</param>
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
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamException"/> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the event stream error.</param>
    /// <param name="correlationId">The optional correlation identifier of the failing operation.</param>
    public EventStreamException(string message, string? correlationId = null)
        : base(message, "EVENT_STREAM_ERROR", correlationId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the event stream error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    /// <param name="correlationId">The optional correlation identifier of the failing operation.</param>
    public EventStreamException(string message, Exception innerException, string? correlationId = null)
        : base(message, "EVENT_STREAM_ERROR", innerException, correlationId)
    {
    }
}
