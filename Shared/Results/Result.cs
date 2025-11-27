// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Results;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public List<string> Errors { get; }

    private Result(bool isSuccess, string? errorCode = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Errors = new List<string>();
    }

    public static Result Success() => new Result(true);

    public static Result Failure(string errorCode, string errorMessage)
        => new Result(false, errorCode, errorMessage);

    public static Result Failure(string errorCode, string errorMessage, List<string> errors)
    {
        var result = new Result(false, errorCode, errorMessage);
        result.Errors.AddRange(errors);
        return result;
    }

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public void ThrowIfFailure()
    {
        if (!IsSuccess)
            throw new InvalidOperationException(ErrorMessage ?? $"Operation failed with code: {ErrorCode}");
    }

    public override string ToString()
        => IsSuccess
            ? "Result { IsSuccess = true }"
            : $"Result {{ IsSuccess = false, ErrorCode = {ErrorCode}, ErrorMessage = {ErrorMessage} }}";
}
