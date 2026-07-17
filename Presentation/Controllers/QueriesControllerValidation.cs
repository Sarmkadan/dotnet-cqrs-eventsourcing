#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// Provides validation helpers for <see cref="QueriesController"/> to ensure
/// controller instances are in a valid state before processing requests.
/// </summary>
public static class QueriesControllerValidation
{
    /// <summary>
    /// Validates the specified <see cref="QueriesController"/> instance.
    /// </summary>
    /// <param name="value">The controller instance to validate.</param>
    /// <returns>A list of human-readable validation problems, or an empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this QueriesController? value)
    {
        if (value is null)
    {
        throw new ArgumentNullException(nameof(value));
    }

        var problems = new List<string>();

        // Validate injected services are not null
        if (value._projectionService is null)
        {
            problems.Add("ProjectionService is null");
        }

        if (value._cacheService is null)
        {
            problems.Add("CacheService is null");
        }

        if (value._logger is null)
        {
            problems.Add("Logger is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="QueriesController"/> instance is valid.
    /// </summary>
    /// <param name="value">The controller instance to check.</param>
    /// <returns>True if the controller is valid; otherwise, false.</returns>
    public static bool IsValid(this QueriesController? value)
    {
        return value is not null && value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="QueriesController"/> instance is valid,
    /// throwing an <see cref="ArgumentException"/> with a detailed message if not.
    /// </summary>
    /// <param name="value">The controller instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the controller has validation problems.</exception>
    public static void EnsureValid(this QueriesController? value)
    {
        if (value is null)
    {
        throw new ArgumentNullException(nameof(value));
    }

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"QueriesController is not valid. Problems: {string.Join(", ", problems)}",
            nameof(value)
        );
    }
}