#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using System.Globalization;

/// <summary>
/// Validation helpers for AccountRepository to ensure repository instances are in a valid state.
/// </summary>
public static class AccountRepositoryValidation
{
    /// <summary>
    /// Validates the AccountRepository instance.
    /// </summary>
    /// <param name="value">The AccountRepository instance to validate.</param>
    /// <returns>A list of human-readable validation problems, or empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this AccountRepository? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // No additional validation needed for AccountRepository as it's a stateless service
        // The repository's state is maintained through the injected IEventRepository
        // and the internal cache, both of which are validated at construction time

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the AccountRepository instance is valid.
    /// </summary>
    /// <param name="value">The AccountRepository instance to check.</param>
    /// <returns>True if the repository is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static bool IsValid(this AccountRepository? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the AccountRepository instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The AccountRepository instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the repository is not valid, containing a list of problems.</exception>
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