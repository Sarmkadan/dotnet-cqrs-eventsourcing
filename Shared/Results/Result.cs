#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Results;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
public sealed class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the machine-readable error code, or <see langword="null"/> for a successful result.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets the human-readable error message, or <see langword="null"/> for a successful result.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the list of accumulated error messages.
    /// </summary>
    public List<string> Errors { get; }

    /// <summary>
    /// Convenience alias for <see cref="ErrorMessage"/>.
    /// </summary>
    public string? Error => ErrorMessage;

    /// <summary>
    /// Payload placeholder for API parity with <see cref="Result{T}"/>.
    /// Non-generic results carry no payload, so this is always <see langword="null"/>.
    /// </summary>
    public object? Data => null;

    private Result(bool isSuccess, string? errorCode = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Errors = new List<string>();
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new Result(true);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="errorMessage">The human-readable error message.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(string errorCode, string errorMessage)
        => new Result(false, errorCode, errorMessage);

    /// <summary>
    /// Creates a failed result with the specified error code, message and list of errors.
    /// </summary>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="errorMessage">The human-readable error message.</param>
    /// <param name="errors">The additional error messages to include.</param>
    /// <returns>A failed <see cref="Result"/> containing the supplied errors.</returns>
    public static Result Failure(string errorCode, string errorMessage, List<string> errors)
    {
        var result = new Result(false, errorCode, errorMessage);
        result.Errors.AddRange(errors);
        return result;
    }

    /// <summary>
    /// Adds an error message to the <see cref="Errors"/> collection.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the result is a failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is not successful.</exception>
    public void ThrowIfFailure()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException(ErrorMessage ?? $"Operation failed with code: {ErrorCode}");
        }
    }

    /// <summary>
    /// Returns a string representation of the result including its success state and error details.
    /// </summary>
    /// <returns>A formatted string describing the result.</returns>
    public override string ToString()
        => IsSuccess
            ? "Result { IsSuccess = true }"
            : $"Result {{ IsSuccess = false, ErrorCode = {ErrorCode}, ErrorMessage = {ErrorMessage} }}";
}
