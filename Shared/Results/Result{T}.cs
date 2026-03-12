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
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public List<string> Errors { get; }

    private Result(bool isSuccess, T? data = default, string? errorCode = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Errors = new List<string>();
    }

    public static Result<T> Success(T data) => new Result<T>(true, data);

    public static Result<T> Failure(string errorCode, string errorMessage)
        => new Result<T>(false, default, errorCode, errorMessage);

    public static Result<T> Failure(string errorCode, string errorMessage, List<string> errors)
    {
        var result = new Result<T>(false, default, errorCode, errorMessage);
        result.Errors.AddRange(errors);
        return result;
    }

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string?, TOut> onFailure)
        => IsSuccess ? onSuccess(Data!) : onFailure(ErrorMessage);

    public void Match(Action<T> onSuccess, Action<string?> onFailure)
    {
        if (IsSuccess)
            onSuccess(Data!);
        else
            onFailure(ErrorMessage);
    }

    public void ThrowIfFailure()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(ErrorMessage ?? $"Operation failed with code: {ErrorCode}");
    }

    public Result<TOut> MapSuccess<TOut>(Func<T, TOut> transform)
        => IsSuccess
            ? Result<TOut>.Success(transform(Data!))
            : Result<TOut>.Failure(ErrorCode!, ErrorMessage!);

    public override string ToString()
        => IsSuccess
            ? $"Result<{typeof(T).Name}> {{ IsSuccess = true, Data = {Data} }}"
            : $"Result<{typeof(T).Name}> {{ IsSuccess = false, ErrorCode = {ErrorCode}, ErrorMessage = {ErrorMessage} }}";
}
