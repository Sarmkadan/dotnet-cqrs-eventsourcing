#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Represents an error that occurs when input, argument, or aggregate validation fails.
/// </summary>
public class ValidationException : DotnetCqrsEventsourcingException
{
    /// <summary>
    /// Gets the validation error messages keyed by the name of the invalid field or argument.
    /// </summary>
    public Dictionary<string, string> ValidationErrors { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the validation failure.</param>
    /// <param name="errorCode">The code that identifies the validation error.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="message"/> or <paramref name="errorCode"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="message"/> or <paramref name="errorCode"/> is <see langword="null"/>.
    /// </exception>
    public ValidationException(string message, string errorCode = "VALIDATION_ERROR")
        : base(message, errorCode)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(message);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorCode);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the validation failure.</param>
    /// <param name="errorCode">The code that identifies the validation error.</param>
    /// <param name="innerException">The exception that caused the validation failure.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="message"/> or <paramref name="errorCode"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="message"/>, <paramref name="errorCode"/>, or <paramref name="innerException"/> is
    /// <see langword="null"/>.
    /// </exception>
    public ValidationException(string message, string errorCode, Exception innerException)
        : base(message, errorCode, innerException)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(message);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorCode);
        global::System.ArgumentNullException.ThrowIfNull(innerException);
    }

    /// <summary>
    /// Adds or replaces the validation error for a field.
    /// </summary>
    /// <param name="fieldName">The name of the invalid field.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>This exception instance, enabling additional errors to be added fluently.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldName"/> or <paramref name="errorMessage"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="fieldName"/> or <paramref name="errorMessage"/> is <see langword="null"/>.
    /// </exception>
    public ValidationException WithError(string fieldName, string errorMessage)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(fieldName);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        ValidationErrors[fieldName] = errorMessage;
        return this;
    }

    /// <summary>
    /// Creates a validation exception for invalid input.
    /// </summary>
    /// <param name="fieldName">The name of the invalid input field.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>A validation exception containing the specified field error.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldName"/> or <paramref name="errorMessage"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="fieldName"/> or <paramref name="errorMessage"/> is <see langword="null"/>.
    /// </exception>
    public static ValidationException InvalidInput(string fieldName, string errorMessage)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(fieldName);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        return new ValidationException("Input validation failed.")
            .WithError(fieldName, errorMessage);
    }

    /// <summary>
    /// Creates a validation exception for an invalid argument.
    /// </summary>
    /// <param name="argumentName">The name of the invalid argument.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>A validation exception containing the specified argument error.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="argumentName"/> or <paramref name="errorMessage"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="argumentName"/> or <paramref name="errorMessage"/> is <see langword="null"/>.
    /// </exception>
    public static ValidationException InvalidArgument(string argumentName, string errorMessage)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(argumentName);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        return new ValidationException("Argument validation failed.")
            .WithError(argumentName, errorMessage);
    }

    /// <summary>
    /// Creates a validation exception for an aggregate that failed validation.
    /// </summary>
    /// <param name="aggregateType">The type name of the invalid aggregate.</param>
    /// <param name="aggregateId">The identifier of the invalid aggregate.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <returns>A validation exception describing the aggregate validation failure.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="aggregateType"/>, <paramref name="aggregateId"/>, or <paramref name="errorMessage"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="aggregateType"/>, <paramref name="aggregateId"/>, or <paramref name="errorMessage"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static ValidationException AggregateValidationFailed(string aggregateType, string aggregateId, string errorMessage)
    {
        global::System.ArgumentException.ThrowIfNullOrEmpty(aggregateType);
        global::System.ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        global::System.ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        return new ValidationException(
            $"Aggregate {aggregateType} with ID '{aggregateId}' failed validation: {errorMessage}");
    }
}
