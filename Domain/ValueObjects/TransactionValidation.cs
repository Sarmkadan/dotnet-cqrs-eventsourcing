#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.ValueObjects;

using System.Globalization;

/// <summary>
/// Provides validation helpers for <see cref="Transaction"/> value objects.
/// </summary>
public static class TransactionValidation
{
    /// <summary>
    /// Validates a <see cref="Transaction"/> instance and returns a list of human-readable validation errors.
    /// </summary>
    /// <param name="value">The transaction to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this Transaction value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            errors.Add("Transaction Id cannot be null or whitespace.");
        }
        else if (value.Id.Length > 100)
        {
            errors.Add("Transaction Id cannot exceed 100 characters.");
        }

        // Validate Type (enum is always valid)

        // Validate Amount - Money constructor already validates amount >= 0 and currency format
        // No null check needed as Money is a class and constructor throws for invalid values

        // Validate TransactionDate
        if (value.TransactionDate == default)
        {
            errors.Add("Transaction TransactionDate cannot be the default DateTime value.");
        }
        else if (value.TransactionDate > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("Transaction TransactionDate cannot be in the future.");
        }
        else if (value.TransactionDate < new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        {
            errors.Add("Transaction TransactionDate cannot be before the year 2000.");
        }

        // Validate Reference
        if (string.IsNullOrWhiteSpace(value.Reference))
        {
            errors.Add("Transaction Reference cannot be null or whitespace.");
        }
        else if (value.Reference.Length > 100)
        {
            errors.Add("Transaction Reference cannot exceed 100 characters.");
        }

        // Validate Description (optional)
        if (value.Description is not null && value.Description.Length > 500)
        {
            errors.Add("Transaction Description cannot exceed 500 characters.");
        }

        // Validate Metadata
        if (value.Metadata is null)
        {
            errors.Add("Transaction Metadata cannot be null.");
        }
        else
        {
            // Check for excessive metadata size
            try
            {
                var metadataSize = System.Text.Json.JsonSerializer.Serialize(value.Metadata).Length;
                if (metadataSize > 10_000) // ~10KB
                {
                    errors.Add("Transaction Metadata cannot exceed approximately 10KB when serialized.");
                }
            }
            catch
            {
                errors.Add("Transaction Metadata contains invalid data that cannot be serialized.");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="Transaction"/> is valid.
    /// </summary>
    /// <param name="value">The transaction to check.</param>
    /// <returns><see langword="true"/> if the transaction is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this Transaction value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="Transaction"/> is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The transaction to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the transaction is invalid, containing the validation errors.</exception>
    public static void EnsureValid(this Transaction value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Transaction is invalid. Validation failed with {errors.Count} error(s):{Environment.NewLine}- ".Replace("\n", "\n- ") +
                string.Join(Environment.NewLine + "- ", errors),
                nameof(value));
        }
    }
}