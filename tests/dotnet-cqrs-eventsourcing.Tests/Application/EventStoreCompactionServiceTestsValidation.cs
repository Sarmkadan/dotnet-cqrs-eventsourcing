#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetCqrsEventSourcing.Tests.Application;

/// <summary>
/// Validation helpers for EventStoreCompactionServiceTests test fixture.
/// </summary>
public static class EventStoreCompactionServiceTestsValidation
{
    /// <summary>
    /// Validates an EventStoreCompactionServiceTests instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The EventStoreCompactionServiceTests instance to validate.</param>
    /// <returns>List of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(this EventStoreCompactionServiceTests value)
    {
        var errors = new List<string>();

        if (value is null)
        {
            errors.Add("EventStoreCompactionServiceTests instance is null.");
            return errors;
        }

        // EventStoreCompactionServiceTests is a test fixture class
        // No additional validation needed beyond null check

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if an EventStoreCompactionServiceTests instance is valid.
    /// </summary>
    /// <param name="value">The EventStoreCompactionServiceTests instance to check.</param>
    /// <returns>True if valid; false otherwise.</returns>
    public static bool IsValid(this EventStoreCompactionServiceTests value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that an EventStoreCompactionServiceTests instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The EventStoreCompactionServiceTests instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid.</exception>
    public static void EnsureValid(this EventStoreCompactionServiceTests value)
    {
        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"EventStoreCompactionServiceTests validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }
}