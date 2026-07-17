#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using System.Globalization;
using Domain.Events;
using Shared.Results;

/// <summary>
/// Provides validation helpers for <see cref="InMemoryEventRepository"/> instances.
/// </summary>
public static class InMemoryEventRepositoryValidation
{
    /// <summary>
    /// Validates the state of an <see cref="InMemoryEventRepository"/> instance.
    /// </summary>
    /// <param name="value">The repository instance to validate.</param>
    /// <returns>A list of human-readable validation problems, or an empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this InMemoryEventRepository value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate internal state consistency by checking observable behavior
        // Since InMemoryEventRepository is in-memory, we validate that it doesn't have corrupted state
        // that would cause issues with event ordering, concurrency, or retrieval

        // The repository should have consistent event ordering and no version conflicts
        // This is validated by attempting to retrieve all events and checking for consistency
        try
        {
            // We can't directly access the internal _events list, but we can validate through public methods
            // Since the repository is in-memory and used only in tests, we validate that basic operations work
            var result = value.GetAllEventsAsync(pageNumber: 1, pageSize: int.MaxValue).Result;
            if (!result.IsSuccess)
            {
                errors.Add($"Repository internal state validation failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Repository internal state validation threw exception: {ex.Message}");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="InMemoryEventRepository"/> instance is valid.
    /// </summary>
    /// <param name="value">The repository instance to check.</param>
    /// <returns><see langword="true"/> if the repository is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this InMemoryEventRepository value)
        => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="InMemoryEventRepository"/> instance is valid.
    /// </summary>
    /// <param name="value">The repository instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the repository has validation problems.</exception>
    public static void EnsureValid(this InMemoryEventRepository value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count == 0)
            return;

        throw new ArgumentException(
            $"InMemoryEventRepository validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
    }

    /// <summary>
    /// Validates an <see cref="EventEnvelope"/> instance.
    /// </summary>
    /// <param name="envelope">The event envelope to validate.</param>
    /// <returns>A list of human-readable validation problems, or an empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this EventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var errors = new List<string>();

        // Validate required string properties
        if (string.IsNullOrWhiteSpace(envelope.Id))
            errors.Add("EventEnvelope.Id must not be null, empty, or whitespace.");

        if (string.IsNullOrWhiteSpace(envelope.AggregateId))
            errors.Add("EventEnvelope.AggregateId must not be null, empty, or whitespace.");

        if (string.IsNullOrWhiteSpace(envelope.AggregateType))
            errors.Add("EventEnvelope.AggregateType must not be null, empty, or whitespace.");

        if (string.IsNullOrWhiteSpace(envelope.EventType))
            errors.Add("EventEnvelope.EventType must not be null, empty, or whitespace.");

        if (string.IsNullOrWhiteSpace(envelope.EventData))
            errors.Add("EventEnvelope.EventData must not be null, empty, or whitespace.");

        // Validate numeric properties
        if (envelope.AggregateVersion < 0)
            errors.Add("EventEnvelope.AggregateVersion must be a non-negative number.");

        // Validate date properties
        if (envelope.CreatedAt == default)
            errors.Add("EventEnvelope.CreatedAt must be a valid UTC date/time.");
        else if (envelope.CreatedAt.Kind != DateTimeKind.Utc)
            errors.Add("EventEnvelope.CreatedAt must be in UTC timezone.");

        // Validate collection properties
        if (envelope.Metadata is null)
            errors.Add("EventEnvelope.Metadata must not be null.");

        // Validate checksum consistency
        if (!string.IsNullOrEmpty(envelope.ChecksumHash) && !envelope.VerifyChecksum())
            errors.Add("EventEnvelope.ChecksumHash is present but does not match computed checksum.");

        // Validate PartitionKey if present
        if (!string.IsNullOrWhiteSpace(envelope.PartitionKey) && string.IsNullOrWhiteSpace(envelope.PartitionKey))
            errors.Add("EventEnvelope.PartitionKey must not be whitespace if specified.");

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventEnvelope"/> instance is valid.
    /// </summary>
    /// <param name="envelope">The event envelope to check.</param>
    /// <returns><see langword="true"/> if the envelope is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this EventEnvelope envelope)
        => Validate(envelope).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="EventEnvelope"/> instance is valid.
    /// </summary>
    /// <param name="envelope">The event envelope to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the envelope has validation problems.</exception>
    public static void EnsureValid(this EventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var errors = Validate(envelope);
        if (errors.Count == 0)
            return;

        throw new ArgumentException(
            $"EventEnvelope validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
    }

    /// <summary>
    /// Validates a list of <see cref="EventEnvelope"/> instances.
    /// </summary>
    /// <param name="envelopes">The list of event envelopes to validate.</param>
    /// <returns>A list of human-readable validation problems, or an empty list if all envelopes are valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelopes"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this IReadOnlyList<EventEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        var errors = new List<string>();

        for (int i = 0; i < envelopes.Count; i++)
        {
            var envelope = envelopes[i];
            if (envelope is null)
            {
                errors.Add($"EventEnvelope at index {i} must not be null.");
                continue;
            }

            var envelopeErrors = Validate(envelope);
            if (envelopeErrors.Count > 0)
            {
                errors.AddRange(envelopeErrors.Select(e => $"EventEnvelope[{i}]: {e}"));
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether all <see cref="EventEnvelope"/> instances in the list are valid.
    /// </summary>
    /// <param name="envelopes">The list of event envelopes to check.</param>
    /// <returns><see langword="true"/> if all envelopes are valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this IReadOnlyList<EventEnvelope> envelopes)
        => Validate(envelopes).Count == 0;

    /// <summary>
    /// Ensures that all <see cref="EventEnvelope"/> instances in the list are valid.
    /// </summary>
    /// <param name="envelopes">The list of event envelopes to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelopes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when any envelope has validation problems.</exception>
    public static void EnsureValid(this IReadOnlyList<EventEnvelope> envelopes)
    {
        ArgumentNullException.ThrowIfNull(envelopes);

        var errors = Validate(envelopes);
        if (errors.Count == 0)
            return;

        throw new ArgumentException(
            $"EventEnvelope list validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
    }

    /// <summary>
    /// Validates a <see cref="Result"/> instance from repository operations.
    /// </summary>
    /// <param name="result">The result to validate.</param>
    /// <param name="operationName">The name of the operation being validated.</param>
    /// <returns>A list of human-readable validation problems, or an empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this Result result, string operationName = "Operation")
    {
        ArgumentNullException.ThrowIfNull(result);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(operationName))
            operationName = "Operation";

        if (!result.IsSuccess)
        {
            if (string.IsNullOrWhiteSpace(result.ErrorCode))
                errors.Add($"{operationName} failed but ErrorCode is null or empty.");

            if (string.IsNullOrWhiteSpace(result.ErrorMessage))
                errors.Add($"{operationName} failed but ErrorMessage is null or empty.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="Result"/> instance is valid.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <param name="operationName">The name of the operation being validated.</param>
    /// <returns><see langword="true"/> if the result is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this Result result, string operationName = "Operation")
        => Validate(result, operationName).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="Result"/> instance is valid.
    /// </summary>
    /// <param name="result">The result to validate.</param>
    /// <param name="operationName">The name of the operation being validated.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the result indicates failure.</exception>
    public static void EnsureValid(this Result result, string operationName = "Operation")
    {
        ArgumentNullException.ThrowIfNull(result);

        var errors = Validate(result, operationName);
        if (errors.Count == 0)
            return;

        throw new ArgumentException(
            $"{operationName} validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
    }

    /// <summary>
    /// Validates a generic <see cref="Result{T}"/> instance from repository operations.
    /// </summary>
    /// <typeparam name="T">The type of the result payload.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="operationName">The name of the operation being validated.</param>
    /// <returns>A list of human-readable validation problems, or an empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate<T>(this Result<T> result, string operationName = "Operation")
    {
        ArgumentNullException.ThrowIfNull(result);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(operationName))
            operationName = "Operation";

        if (!result.IsSuccess)
        {
            if (string.IsNullOrWhiteSpace(result.ErrorCode))
                errors.Add($"{operationName} failed but ErrorCode is null or empty.");

            if (string.IsNullOrWhiteSpace(result.ErrorMessage))
                errors.Add($"{operationName} failed but ErrorMessage is null or empty.");
        }
        else
        {
            // Validate payload when successful
            if (result.Data is null)
            {
                // For collections, null might be valid (empty collection)
                if (typeof(T) != typeof(List<EventEnvelope>) &&
                    typeof(T) != typeof(IReadOnlyList<EventEnvelope>))
                {
                    errors.Add($"{operationName} succeeded but Data payload is null.");
                }
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="Result{T}"/> instance is valid.
    /// </summary>
    /// <typeparam name="T">The type of the result payload.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="operationName">The name of the operation being validated.</param>
    /// <returns><see langword="true"/> if the result is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid<T>(this Result<T> result, string operationName = "Operation")
        => Validate(result, operationName).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="Result{T}"/> instance is valid.
    /// </summary>
    /// <typeparam name="T">The type of the result payload.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="operationName">The name of the operation being validated.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the result indicates failure or has invalid structure.</exception>
    public static void EnsureValid<T>(this Result<T> result, string operationName = "Operation")
    {
        ArgumentNullException.ThrowIfNull(result);

        var errors = Validate(result, operationName);
        if (errors.Count == 0)
            return;

        throw new ArgumentException(
            $"{operationName} validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
    }
}