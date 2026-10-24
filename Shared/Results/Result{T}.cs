#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Results;

/// <summary>
/// Represents the result of an operation that can succeed with data or fail.
/// </summary>
public class Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the data produced by a successful operation, or <see langword="default"/> for a failure.
    /// </summary>
    public T? Data { get; }

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

    private Result(bool isSuccess, T? data = default, string? errorCode = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Errors = new List<string>();
    }

    /// <summary>
    /// Creates a successful result carrying the specified data.
    /// </summary>
    /// <param name="data">The data produced by the operation.</param>
    /// <returns>A successful <see cref="Result{T}"/> containing <paramref name="data"/>.</returns>
    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data);
    }

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="errorMessage">The human-readable error message.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(string errorCode, string errorMessage)
        => new Result<T>(false, default, errorCode, errorMessage);

    /// <summary>
    /// Creates a failed result with the specified error code, message and list of errors.
    /// </summary>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="errorMessage">The human-readable error message.</param>
    /// <param name="errors">The additional error messages to include.</param>
    /// <returns>A failed <see cref="Result{T}"/> containing the supplied errors.</returns>
    public static Result<T> Failure(string errorCode, string errorMessage, List<string> errors)
    {
        var result = new Result<T>(false, default, errorCode, errorMessage);
        result.Errors.AddRange(errors);
        return result;
    }

    /// <summary>
    /// Adds an error message to the <see cref="Errors"/> collection.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
    public void AddError(string error)
    {
        if (error == null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Errors.Add(error);
    }

    /// <summary>
    /// Invokes the success or failure function depending on the state of the result and returns its value.
    /// </summary>
    /// <typeparam name="TOut">The type of the value returned by the match functions.</typeparam>
    /// <param name="onSuccess">The function invoked with the data when the result is successful.</param>
    /// <param name="onFailure">The function invoked with the error message when the result is a failure.</param>
    /// <returns>The value returned by the invoked function.</returns>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string?, TOut> onFailure)
        => IsSuccess ? onSuccess(Data!) : onFailure(ErrorMessage);

    /// <summary>
    /// Invokes the success or failure action depending on the state of the result.
    /// </summary>
    /// <param name="onSuccess">The action invoked with the data when the result is successful.</param>
    /// <param name="onFailure">The action invoked with the error message when the result is a failure.</param>
    public void Match(Action<T> onSuccess, Action<string?> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess(Data!);
        }
        else
        {
            onFailure(ErrorMessage);
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the result is a failure or carries no data.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is not successful or <see cref="Data"/> is <see langword="null"/>.</exception>
    public void ThrowIfFailure()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException(ErrorMessage ?? $"Operation failed with code: {ErrorCode}");
        }

        if (Data == null)
        {
            throw new InvalidOperationException("Data is null");
        }
    }

    /// <summary>
    /// Transforms the data of a successful result using the specified function, preserving failures.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed data.</typeparam>
    /// <param name="transform">The function that transforms the data.</param>
    /// <returns>A successful <see cref="Result{TOut}"/> with the transformed data, or a failure carrying the original error.</returns>
    public Result<TOut> MapSuccess<TOut>(Func<T, TOut> transform)
        => IsSuccess
            ? Result<TOut>.Success(transform(Data!))
            : Result<TOut>.Failure(ErrorCode!, ErrorMessage!);

    /// <summary>
    /// Returns a string representation of the result including its success state, data or error details.
    /// </summary>
    /// <returns>A formatted string describing the result.</returns>
    public override string ToString()
        => IsSuccess
            ? $"Result<{typeof(T).Name}> {{ IsSuccess = true, Data = {Data} }}"
            : $"Result<{typeof(T).Name}> {{ IsSuccess = false, ErrorCode = {ErrorCode}, ErrorMessage = {ErrorMessage} }}";
}
