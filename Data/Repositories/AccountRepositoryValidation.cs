#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using System;

/// <summary>
/// Validation helpers for AccountRepository to ensure repository instances are in a valid state.
/// </summary>
public static class AccountRepositoryValidation
{
    /// <summary>
    /// Validates the AccountRepository instance.
    /// </summary>
    /// <param name="value">The AccountRepository instance to validate.</param>
    /// <returns>An empty read-only list if the repository is valid; otherwise a list of validation problems.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static System.Collections.Generic.IReadOnlyList<string> Validate(this AccountRepository? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether the AccountRepository instance is valid.
    /// </summary>
    /// <param name="value">The AccountRepository instance to check.</param>
    /// <returns>True if the repository is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this AccountRepository? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the AccountRepository instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The AccountRepository instance to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The repository is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this AccountRepository? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"AccountRepository is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}