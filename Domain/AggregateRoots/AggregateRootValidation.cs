#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.AggregateRoots;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides validation helpers for <see cref="AggregateRoot"/> instances.
/// </summary>
public static class AggregateRootValidation
{
    /// <summary>
    /// Validates the specified aggregate root instance.
    /// </summary>
    /// <param name="value">The aggregate root to validate.</param>
    /// <returns>A list of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this AggregateRoot? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            problems.Add($"AggregateRoot.Id cannot be null, empty, or whitespace. Actual: '{value.Id}'");
        }
        else if (value.Id.Length > 100)
        {
            problems.Add($"AggregateRoot.Id length exceeds 100 characters. Actual: {value.Id.Length}");
        }

        // Validate Version
        if (value.Version < 0)
        {
            problems.Add($"AggregateRoot.Version cannot be negative. Actual: {value.Version}");
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            problems.Add("AggregateRoot.CreatedAt cannot be default(DateTime).");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("AggregateRoot.CreatedAt cannot be in the future.");
        }
        else if (value.CreatedAt.Kind != DateTimeKind.Utc)
        {
            problems.Add("AggregateRoot.CreatedAt must be in UTC. Use DateTime.UtcNow.");
        }

        // Validate UpdatedAt
        if (value.UpdatedAt == default)
        {
            problems.Add("AggregateRoot.UpdatedAt cannot be default(DateTime).");
        }
        else if (value.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("AggregateRoot.UpdatedAt cannot be in the future.");
        }
        else if (value.UpdatedAt.Kind != DateTimeKind.Utc)
        {
            problems.Add("AggregateRoot.UpdatedAt must be in UTC. Use DateTime.UtcNow.");
        }

        // Validate CreatedAt <= UpdatedAt
        if (value.CreatedAt != default && value.UpdatedAt != default && value.CreatedAt > value.UpdatedAt)
        {
            problems.Add("AggregateRoot.CreatedAt cannot be after UpdatedAt.");
        }

        // Validate TenantId if set
        if (value.TenantId is not null)
        {
            if (string.IsNullOrWhiteSpace(value.TenantId))
            {
                problems.Add($"AggregateRoot.TenantId cannot be null, empty, or whitespace when set. Actual: '{value.TenantId}'");
            }
            else if (value.TenantId.Length > 50)
            {
                problems.Add($"AggregateRoot.TenantId length exceeds 50 characters. Actual: {value.TenantId.Length}");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified aggregate root is valid.
    /// </summary>
    /// <param name="value">The aggregate root to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this AggregateRoot? value)
        => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that the specified aggregate root is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The aggregate root to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the aggregate root is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid(this AggregateRoot? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"AggregateRoot validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }
}