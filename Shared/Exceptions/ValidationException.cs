#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

public class ValidationException : DotnetCqrsEventsourcingException
{
    public Dictionary<string, string> ValidationErrors { get; } = new();

    public ValidationException(string message, string errorCode = "VALIDATION_ERROR")
        : base(message, errorCode)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(message);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorCode);
    }

    public ValidationException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, innerException)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(message);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorCode);
        global::System.ArgumentNullException.ThrowIfNull(innerException);
    }

    public ValidationException WithError(string fieldName, string errorMessage)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(fieldName);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        ValidationErrors[fieldName] = errorMessage;
        return this;
    }

    public static ValidationException InvalidInput(string fieldName, string errorMessage)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(fieldName);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        return new ValidationException("Input validation failed.")
            .WithError(fieldName, errorMessage);
    }

    public static ValidationException InvalidArgument(string argumentName, string errorMessage)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(argumentName);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        return new ValidationException("Argument validation failed.")
            .WithError(argumentName, errorMessage);
    }

    public static ValidationException AggregateValidationFailed(string aggregateType, string aggregateId, string errorMessage)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(aggregateType);
        global::System.ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        return new ValidationException(
            $"Aggregate {aggregateType} with ID '{aggregateId}' failed validation: {errorMessage}");
    }
}