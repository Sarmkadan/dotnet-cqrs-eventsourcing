#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Exception thrown when input validation fails.
/// </summary>
public class ValidationException : DotnetCqrsEventsourcingException
{
    public Dictionary<string, string> ValidationErrors { get; } = new();

    public ValidationException(string message, string errorCode = "VALIDATION_ERROR")
        : base(message, errorCode)
    {
    }

    public ValidationException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, innerException)
    {
    }

    public ValidationException WithError(string fieldName, string errorMessage)
    {
        ValidationErrors[fieldName] = errorMessage;
        return this;
    }

    public static ValidationException InvalidInput(string fieldName, string errorMessage)
        => new ValidationException("Input validation failed.")
            .WithError(fieldName, errorMessage);

    public static ValidationException InvalidArgument(string argumentName, string errorMessage)
        => new ValidationException("Argument validation failed.")
            .WithError(argumentName, errorMessage);

    public static ValidationException AggregateValidationFailed(string aggregateType, string aggregateId, string errorMessage)
        => new ValidationException(
            $"Aggregate {aggregateType} with ID '{aggregateId}' failed validation: {errorMessage}");
}