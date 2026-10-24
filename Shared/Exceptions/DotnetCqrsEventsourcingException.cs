#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Base exception for all custom exceptions in the DotNetCqrsEventSourcing framework.
/// </summary>
public class DotnetCqrsEventsourcingException : Exception
{
    /// <summary>
    /// Gets the machine-readable code identifying the error.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the UTC date and time at which the exception was created.
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetCqrsEventsourcingException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The machine-readable error code. Defaults to <c>GENERIC_ERROR</c>.</param>
    public DotnetCqrsEventsourcingException(string message, string errorCode = "GENERIC_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetCqrsEventsourcingException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="innerException">The exception that caused the current exception, or <see langword="null"/>.</param>
    public DotnetCqrsEventsourcingException(string message, string errorCode, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Returns a string representation of the exception prefixed with its error code.
    /// </summary>
    /// <returns>A formatted string describing the exception.</returns>
    public override string ToString()
    {
        return $"[{ErrorCode}] {base.ToString()}";
    }
}