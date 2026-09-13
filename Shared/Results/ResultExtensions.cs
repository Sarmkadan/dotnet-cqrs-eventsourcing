#nullable enable
namespace DotNetCqrsEventSourcing.Shared.Results;

/// <summary>
/// Extension methods for <see cref="Result"/>.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Executes the specified action if the result is successful.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute on success.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute on failure, receiving the error message.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result OnFailure(this Result result, Action<string> action)
    {
        if (!result.IsSuccess)
        {
            action(result.ErrorMessage);
        }

        return result;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="action">The action to execute on failure, receiving the result.</param>
    /// <returns>The original result for chaining.</returns>
    public static Result OnFailure(this Result result, Action<Result> action)
    {
        if (!result.IsSuccess)
        {
            action(result);
        }

        return result;
    }
}