#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetCqrsEventSourcing.Tests.Application;

/// <summary>
/// Validation helpers for EventStoreCompactionServiceTests test fixture.
/// Validates that the test fixture is properly initialized with required dependencies.
/// </summary>
public static class EventStoreCompactionServiceTestsValidation
{
    /// <summary>
    /// Validates an EventStoreCompactionServiceTests instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The EventStoreCompactionServiceTests instance to validate.</param>
    /// <returns>List of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EventStoreCompactionServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate repository is initialized
        if (value._repository is null)
        {
            errors.Add("Repository (_repository) is not initialized.");
        }

        // Validate snapshot service mock is initialized
        if (value._snapshotMock is null)
        {
            errors.Add("Snapshot service mock (_snapshotMock) is not initialized.");
        }

        // Validate service under test is initialized
        if (value._sut is null)
        {
            errors.Add("Service under test (_sut) is not initialized.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if an EventStoreCompactionServiceTests instance is valid.
    /// </summary>
    /// <param name="value">The EventStoreCompactionServiceTests instance to check.</param>
    /// <returns>True if valid; false otherwise.</returns>
    public static bool IsValid(this EventStoreCompactionServiceTests? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that an EventStoreCompactionServiceTests instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The EventStoreCompactionServiceTests instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid.</exception>
    public static void EnsureValid(this EventStoreCompactionServiceTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"EventStoreCompactionServiceTests validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }
}