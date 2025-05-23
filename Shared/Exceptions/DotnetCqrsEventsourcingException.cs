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
    public string ErrorCode { get; }
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public DotnetCqrsEventsourcingException(string message, string errorCode = "GENERIC_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DotnetCqrsEventsourcingException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public override string ToString()
    {
        return $"[{ErrorCode}] {base.ToString()}";
    }
}