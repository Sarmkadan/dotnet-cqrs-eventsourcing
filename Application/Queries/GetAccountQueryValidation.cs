#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Queries;

/// <summary>
/// Validation helpers for <see cref="GetAccountQuery"/>.
/// </summary>
public static class GetAccountQueryValidation
{
    /// <summary>
    /// Validates the <see cref="GetAccountQuery"/> instance.
    /// </summary>
    /// <param name="value">The query to validate.</param>
    /// <returns>A list of validation error messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this GetAccountQuery value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate AccountId
        if (string.IsNullOrWhiteSpace(value.AccountId))
        {
            errors.Add("AccountId cannot be null or whitespace.");
        }
        else if (value.AccountId.Length > 100)
        {
            errors.Add("AccountId cannot exceed 100 characters.");
        }

        // Validate CorrelationId
        if (string.IsNullOrWhiteSpace(value.CorrelationId))
        {
            errors.Add("CorrelationId cannot be null or whitespace.");
        }
        else if (!Guid.TryParse(value.CorrelationId, out _))
        {
            errors.Add("CorrelationId must be a valid GUID.");
        }

        // Validate IssuedAt
        if (value.IssuedAt == default)
        {
            errors.Add("IssuedAt must be a valid DateTime.");
        }
        else if (value.IssuedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("IssuedAt cannot be in the future.");
        }
        else if (value.IssuedAt < DateTime.UtcNow.AddYears(-1))
        {
            errors.Add("IssuedAt cannot be older than one year.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="GetAccountQuery"/> instance is valid.
    /// </summary>
    /// <param name="value">The query to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(this GetAccountQuery value)
        => value.Validate().Count == 0;

    /// <summary>
    /// Ensures the <see cref="GetAccountQuery"/> instance is valid.
    /// </summary>
    /// <param name="value">The query to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the query is invalid, containing all validation errors.</exception>
    public static void EnsureValid(this GetAccountQuery value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"GetAccountQuery validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }
}