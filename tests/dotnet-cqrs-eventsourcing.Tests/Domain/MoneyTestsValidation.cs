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
    /// Validates that a Money instance can be constructed without throwing exceptions.
    /// </summary>
    /// <param name="amount">The amount to validate.</param>
    /// <param name="currency">The currency code to validate.</param>
    /// <returns>List of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="currency"/> is null.</exception>
    public static IReadOnlyList<string> Validate(decimal amount, string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        var errors = new List<string>();

        if (amount < 0)
        {
            errors.Add("Amount cannot be negative.");
        }

        if (amount > CqrsConstants.MaximumBalance)
        {
            errors.Add($"Amount cannot exceed maximum balance of {CqrsConstants.MaximumBalance}.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            errors.Add("Currency cannot be null or whitespace.");
        }
        else if (currency.Length != 3)
        {
            errors.Add("Currency must be a 3-character ISO code.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates that a Money instance is valid by attempting to construct it.
    /// </summary>
    /// <param name="value">The Money instance to validate.</param>
    /// <returns>True if the Money instance is valid; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this Money? value)
    {
        if (value is null)
        {
            return false;
        }

        try
        {
            _ = new Money(value.Amount, value.Currency);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that a Money instance is valid by attempting to reconstruct it,
    /// throwing an exception if validation fails.
    /// </summary>
    /// <param name="value">The Money instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid.</exception>
    public static void EnsureValid(this Money? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            _ = new Money(value.Amount, value.Currency);
        }
        catch (DomainException ex) when (ex is not null)
        {
            throw new ArgumentException(
                $"Money validation failed: {ex.Message}", nameof(value));
        }
    }

    /// <summary>
    /// Validates Money constructor parameters and returns a list of validation problems.
    /// </summary>
    /// <param name="amount">The amount to validate.</param>
    /// <param name="currency">The currency code to validate.</param>
    /// <returns>List of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(decimal amount, ReadOnlySpan<char> currency)
    {
        var errors = new List<string>();

        if (amount < 0)
        {
            errors.Add("Amount cannot be negative.");
        }

        if (amount > CqrsConstants.MaximumBalance)
        {
            errors.Add($"Amount cannot exceed maximum balance of {CqrsConstants.MaximumBalance}.");
        }

        if (currency.IsEmpty || currency.IsWhiteSpace())
        {
            errors.Add("Currency cannot be null or whitespace.");
        }
        else if (currency.Length != 3)
        {
            errors.Add("Currency must be a 3-character ISO code.");
        }

        return errors.AsReadOnly();
    }
}
