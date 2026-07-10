#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetCqrsEventSourcing.Tests.Domain;

using DotNetCqrsEventSourcing.Domain.ValueObjects;
using DotNetCqrsEventSourcing.Shared.Constants;
using DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Validation helpers for Money value objects.
/// </summary>
public static class MoneyTestsValidation
{
    /// <summary>
    /// Validates a Money instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The Money instance to validate.</param>
    /// <returns>List of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(this Money value)
    {
        var errors = new List<string>();

        if (value is null)
        {
            errors.Add("Money instance is null.");
            return errors;
        }

        if (value.Amount < 0)
        {
            errors.Add("Amount cannot be negative.");
        }

        if (value.Amount > CqrsConstants.MaximumBalance)
        {
            errors.Add($"Amount cannot exceed maximum balance of {CqrsConstants.MaximumBalance}.");
        }

        if (string.IsNullOrWhiteSpace(value.Currency))
        {
            errors.Add("Currency cannot be null or whitespace.");
        }
        else if (value.Currency.Length != 3)
        {
            errors.Add("Currency must be a 3-character ISO code.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if a Money instance is valid.
    /// </summary>
    /// <param name="value">The Money instance to check.</param>
    /// <returns>True if valid; false otherwise.</returns>
    public static bool IsValid(this Money value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a Money instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The Money instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid.</exception>
    public static void EnsureValid(this Money value)
    {
        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Money validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }
}
