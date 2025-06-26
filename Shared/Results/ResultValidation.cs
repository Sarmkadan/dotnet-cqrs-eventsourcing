#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotNetCqrsEventSourcing.Shared.Results;

/// <summary>
/// Provides validation helpers for <see cref="Result"/> and <see cref="Result{T}"/> types.
/// </summary>
public static class ResultValidation
{
    /// <summary>
    /// Validates a <see cref="Result"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The result to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the result is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this Result value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (!value.IsSuccess)
        {
            if (string.IsNullOrWhiteSpace(value.ErrorCode))
            {
                problems.Add("Non-successful result must have a non-null, non-empty ErrorCode.");
            }

            if (string.IsNullOrWhiteSpace(value.ErrorMessage))
            {
                problems.Add("Non-successful result must have a non-null, non-empty ErrorMessage.");
            }
        }

        // Validate Errors collection
        if (value.Errors is null)
        {
            problems.Add("Errors collection must not be null.");
        }
        else if (value.Errors.Any(e => string.IsNullOrWhiteSpace(e)))
        {
            problems.Add("Errors collection must not contain null or whitespace entries.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a <see cref="Result{T}"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the result.</typeparam>
    /// <param name="value">The result to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the result is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate<T>(this Result<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (!value.IsSuccess)
        {
            if (string.IsNullOrWhiteSpace(value.ErrorCode))
            {
                problems.Add("Non-successful result must have a non-null, non-empty ErrorCode.");
            }

            if (string.IsNullOrWhiteSpace(value.ErrorMessage))
            {
                problems.Add("Non-successful result must have a non-null, non-empty ErrorMessage.");
            }
        }
        else
        {
            // Validate success case: Data should not be null for value types, but can be null for reference types
            if (value.Data is null && default(T) != null)
            {
                problems.Add("Successful result with non-nullable type must have non-null Data.");
            }
        }

        // Validate Errors collection
        if (value.Errors is null)
        {
            problems.Add("Errors collection must not be null.");
        }
        else if (value.Errors.Any(e => string.IsNullOrWhiteSpace(e)))
        {
            problems.Add("Errors collection must not contain null or whitespace entries.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="Result"/> is valid.
    /// </summary>
    /// <param name="value">The result to check.</param>
    /// <returns><see langword="true"/> if the result is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this Result value) => Validate(value).Count == 0;

    /// <summary>
    /// Determines whether the specified <see cref="Result{T}"/> is valid.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the result.</typeparam>
    /// <param name="value">The result to check.</param>
    /// <returns><see langword="true"/> if the result is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid<T>(this Result<T> value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="Result"/> is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all validation problems if it is not.
    /// </summary>
    /// <param name="value">The result to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the result is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this Result value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Result validation failed:{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", problems)
                }",
                nameof(value));
        }
    }

    /// <summary>
    /// Ensures that the specified <see cref="Result{T}"/> is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all validation problems if it is not.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the result.</typeparam>
    /// <param name="value">The result to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the result is not valid, containing a list of problems.</exception>
    public static void EnsureValid<T>(this Result<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Result<T> validation failed:{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", problems)
                }",
                nameof(value));
        }
    }
}
