#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides validation helpers for <see cref="DeadLetterEntry"/> instances.
/// </summary>
public static class DeadLetterEntryValidation
{
    /// <summary>
    /// Validates a <see cref="DeadLetterEntry"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The entry to validate.</param>
    /// <returns>A read-only list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this DeadLetterEntry? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            problems.Add("Id must not be null, empty, or whitespace.");
        }
        else if (!IsValidGuidFormat(value.Id))
        {
            problems.Add("Id must be a valid GUID format.");
        }

        // Validate Event
        if (value.Event is null)
        {
            problems.Add("Event must not be null.");
        }

        // Validate ProjectionName
        if (string.IsNullOrWhiteSpace(value.ProjectionName))
        {
            problems.Add("ProjectionName must not be null, empty, or whitespace.");
        }

        // Validate ErrorMessage
        if (string.IsNullOrWhiteSpace(value.ErrorMessage))
        {
            problems.Add("ErrorMessage must not be null, empty, or whitespace.");
        }

        // Validate AttemptCount
        if (value.AttemptCount < 0)
        {
            problems.Add("AttemptCount must be a non-negative integer.");
        }

        // Validate FailedAt
        if (value.FailedAt == default)
        {
            problems.Add("FailedAt must be a valid UTC timestamp (cannot be default DateTime).");
        }
        else if (value.FailedAt.Kind != DateTimeKind.Utc)
        {
            problems.Add("FailedAt must be in UTC timezone.");
        }
        else if (value.FailedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("FailedAt cannot be in the future.");
        }

        // Validate reprocessing state consistency
        if (value.IsReprocessed && value.ReprocessedAt is null)
        {
            problems.Add("If IsReprocessed is true, ReprocessedAt must not be null.");
        }
        else if (!value.IsReprocessed && value.ReprocessedAt is not null)
        {
            problems.Add("If IsReprocessed is false, ReprocessedAt must be null.");
        }

        if (value.ReprocessedAt.HasValue)
        {
            if (value.ReprocessedAt.Value == default)
            {
                problems.Add("ReprocessedAt must be a valid UTC timestamp (cannot be default DateTime).");
            }
            else if (value.ReprocessedAt.Value.Kind != DateTimeKind.Utc)
            {
                problems.Add("ReprocessedAt must be in UTC timezone.");
            }
            else if (value.ReprocessedAt.Value > DateTime.UtcNow.AddMinutes(5))
            {
                problems.Add("ReprocessedAt cannot be in the future.");
            }
            else if (value.ReprocessedAt.Value < value.FailedAt)
            {
                problems.Add("ReprocessedAt cannot be earlier than FailedAt.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="DeadLetterEntry"/> is valid.
    /// </summary>
    /// <param name="value">The entry to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this DeadLetterEntry? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="DeadLetterEntry"/> is valid, throwing an <see cref="ArgumentException"/>
    /// with the list of validation problems if it is not.
    /// </summary>
    /// <param name="value">The entry to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the entry is invalid, containing the validation problems.</exception>
    public static void EnsureValid(this DeadLetterEntry? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                "DeadLetterEntry validation failed:\n" + string.Join("\n", problems),
                nameof(value));
        }
    }

    /// <summary>
    /// Determines whether a string represents a valid GUID format.
    /// </summary>
    /// <param name="input">The string to check.</param>
    /// <returns>True if valid GUID format; otherwise, false.</returns>
    private static bool IsValidGuidFormat(string input)
    {
        return Guid.TryParse(input, out _);
    }
}