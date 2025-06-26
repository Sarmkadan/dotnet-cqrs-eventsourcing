#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Globalization;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides validation helpers for <see cref="ReadModelProjectionEngine"/> instances.
/// </summary>
public static class ReadModelProjectionEngineValidation
{
    /// <summary>
    /// Validates the specified <see cref="ReadModelProjectionEngine"/> instance.
    /// </summary>
    /// <param name="value">The engine instance to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this ReadModelProjectionEngine value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate internal state consistency
        if (value.Checkpoints is null)
            problems.Add("Checkpoints dictionary is null.");
        else if (value.Checkpoints.Count > 0)
        {
            foreach (var checkpoint in value.Checkpoints.Values)
            {
                if (checkpoint is null)
                    problems.Add("Checkpoint in collection is null.");
                else
                    problems.AddRange(Validate(checkpoint));
            }
        }

        if (value.TotalEventsRouted < 0)
            problems.Add("TotalEventsRouted cannot be negative.");

        // Validate internal collections
        if (value.GetType().GetField("_checkpoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value) is not ConcurrentDictionary<string, ProjectionCheckpoint> checkpoints)
            problems.Add("Internal checkpoints collection is not a ConcurrentDictionary<string, ProjectionCheckpoint>.");

        if (value.GetType().GetField("_processingCounters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(value) is not ConcurrentDictionary<string, long> processingCounters)
            problems.Add("Internal processing counters collection is not a ConcurrentDictionary<string, long>.");

        if (value.GetType().GetField("_totalEventsRouted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) is null)
            problems.Add("Internal total events routed field is missing.");

        if (value.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) is null)
            problems.Add("Internal disposed flag field is missing.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates the specified <see cref="ProjectionCheckpoint"/> instance.
    /// </summary>
    /// <param name="checkpoint">The checkpoint to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    private static IReadOnlyList<string> Validate(this ProjectionCheckpoint checkpoint)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(checkpoint.ProjectionName))
            problems.Add("ProjectionName is null or whitespace.");

        if (string.IsNullOrWhiteSpace(checkpoint.LastProcessedEventId))
            problems.Add("LastProcessedEventId is null or whitespace.");

        if (checkpoint.LastProcessedVersion <= 0)
            problems.Add("LastProcessedVersion must be positive.");

        if (checkpoint.WrittenAt == default)
            problems.Add("WrittenAt is default (uninitialized).");

        if (checkpoint.TotalEventsProcessed < 0)
            problems.Add("TotalEventsProcessed cannot be negative.");

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ReadModelProjectionEngine"/> instance is valid.
    /// </summary>
    /// <param name="value">The engine instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ReadModelProjectionEngine value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ReadModelProjectionEngine"/> instance is valid.
    /// </summary>
    /// <param name="value">The engine instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this ReadModelProjectionEngine value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
            return;

        throw new ArgumentException(
            $"ReadModelProjectionEngine is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
    }
}