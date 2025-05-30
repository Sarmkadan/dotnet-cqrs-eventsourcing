#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides useful extension methods for <see cref="ReadModelProjectionEngine"/> to simplify
/// common projection management tasks such as checkpoint inspection, status monitoring,
/// and batch operations.
/// </summary>
public static class ReadModelProjectionEngineExtensions
{
    /// <summary>
    /// Gets the checkpoint for the specified projection, or creates a new empty checkpoint
    /// if none exists. Useful for ensuring a checkpoint always exists before operations.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <param name="projectionName">Name of the projection to get or create checkpoint for.</param>
    /// <returns>A checkpoint instance, either existing or newly created.</returns>
    public static ProjectionCheckpoint GetOrCreateCheckpoint(
        this ReadModelProjectionEngine engine,
        string projectionName)
    {
        var checkpoint = engine.GetCheckpoint(projectionName);

        if (checkpoint is not null)
            return checkpoint;

        return new ProjectionCheckpoint(
            projectionName,
            string.Empty,
            0,
            DateTime.UtcNow,
            0
        );
    }

    /// <summary>
    /// Checks whether all registered projections have reached a specified aggregate version.
    /// Useful for verifying that projections are caught up to a specific point.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <param name="targetVersion">The aggregate version to check against.</param>
    /// <returns>
    /// <see langword="true"/> if all projections have reached or exceeded the target version;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool AllProjectionsAtVersionOrHigher(
        this ReadModelProjectionEngine engine,
        long targetVersion)
    {
        if (engine.Checkpoints.Count == 0)
            return false;

        return engine.Checkpoints.Values.All(cp => cp.LastProcessedVersion >= targetVersion);
    }

    /// <summary>
    /// Gets the names of all projections that have checkpoints.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <returns>An enumerable of projection names with checkpoints.</returns>
    public static IEnumerable<string> GetProjectionNamesWithCheckpoints(
        this ReadModelProjectionEngine engine)
    {
        return engine.Checkpoints.Keys;
    }

    /// <summary>
    /// Gets the total number of events processed across all projections.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <returns>The sum of all events processed by all projections.</returns>
    public static long GetTotalEventsProcessed(
        this ReadModelProjectionEngine engine)
    {
        return engine.Checkpoints.Values.Sum(cp => cp.TotalEventsProcessed);
    }

    /// <summary>
    /// Checks if any projection has failed events that need attention.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <returns>
    /// <see langword="true"/> if any projection has encountered errors during processing;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool HasAnyProjectionErrors(
        this ReadModelProjectionEngine engine)
    {
        return engine.Checkpoints.Values.Any(cp => cp.TotalEventsProcessed > 0 && cp.LastProcessedVersion == 0);
    }

    /// <summary>
    /// Gets the most recent event ID processed by the specified projection.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <param name="projectionName">Name of the projection to query.</param>
    /// <returns>
    /// The event ID of the most recent event processed, or <see langword="null"/>
    /// if the projection has no checkpoint or hasn't processed any events yet.
    /// </returns>
    public static string? GetLastProcessedEventId(
        this ReadModelProjectionEngine engine,
        string projectionName)
    {
        return engine.GetCheckpoint(projectionName)?.LastProcessedEventId;
    }

    /// <summary>
    /// Gets the timestamp when the specified projection was last updated.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <param name="projectionName">Name of the projection to query.</param>
    /// <returns>
    /// The UTC timestamp when the projection was last updated, or <see cref="DateTime.MinValue"/>
    /// if the projection has no checkpoint.
    /// </returns>
    public static DateTime GetLastUpdatedTimestamp(
        this ReadModelProjectionEngine engine,
        string projectionName)
    {
        return engine.GetCheckpoint(projectionName)?.WrittenAt ?? DateTime.MinValue;
    }

    /// <summary>
    /// Determines if a specific projection is actively processing events.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <param name="projectionName">Name of the projection to check.</param>
    /// <returns>
    /// <see langword="true"/> if the projection has processed at least one event and has a valid checkpoint;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsProjectionActive(
        this ReadModelProjectionEngine engine,
        string projectionName)
    {
        var checkpoint = engine.GetCheckpoint(projectionName);
        return checkpoint is not null && checkpoint.TotalEventsProcessed > 0;
    }

    /// <summary>
    /// Gets a summary of projection status including checkpoint information.
    /// </summary>
    /// <param name="engine">The projection engine instance.</param>
    /// <returns>
    /// A dictionary mapping projection names to their status information tuples containing:
    /// (LastProcessedEventId, LastProcessedVersion, TotalEventsProcessed, WrittenAt)
    /// </returns>
    public static IReadOnlyDictionary<string, (string? LastEventId, long Version, long TotalProcessed, DateTime WrittenAt)>
        GetProjectionStatusSummary(
        this ReadModelProjectionEngine engine)
    {
        var result = new Dictionary<string, (string?, long, long, DateTime)>(engine.Checkpoints.Count);

        foreach (var kvp in engine.Checkpoints)
        {
            var checkpoint = kvp.Value;
            result[kvp.Key] = (
                checkpoint.LastProcessedEventId,
                checkpoint.LastProcessedVersion,
                checkpoint.TotalEventsProcessed,
                checkpoint.WrittenAt
            );
        }

        return result;
    }
}