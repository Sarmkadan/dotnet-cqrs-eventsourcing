#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides diagnostic information about the read-model projection engine,
/// including per-projection event counts, checkpoint positions, and dead-letter
/// entry counts.
/// </summary>
public sealed class ProjectionDiagnosticsService
{
    private readonly ReadModelProjectionEngine _engine;
    private readonly IDeadLetterStore _deadLetterStore;

    /// <summary>Initialises the service with the required dependencies.</summary>
    public ProjectionDiagnosticsService(
        ReadModelProjectionEngine engine,
        IDeadLetterStore deadLetterStore)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _deadLetterStore = deadLetterStore ?? throw new ArgumentNullException(nameof(deadLetterStore));
    }

    /// <summary>Total number of events routed since the engine started.</summary>
    public long TotalEventsRouted => _engine.TotalEventsRouted;

    /// <summary>Read-only snapshot of all current projection checkpoints.</summary>
    public IReadOnlyDictionary<string, ProjectionCheckpoint> Checkpoints => _engine.Checkpoints;

    /// <summary>
    /// Returns the number of unprocessed dead-letter entries currently in the store.
    /// </summary>
    public Task<int> GetDeadLetterCountAsync(CancellationToken cancellationToken = default)
        => _deadLetterStore.GetCountAsync(cancellationToken);

    /// <summary>
    /// Returns all unprocessed dead-letter entries for the specified projection.
    /// </summary>
    public Task<IReadOnlyList<DeadLetterEntry>> GetDeadLetterEntriesAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
        => _deadLetterStore.GetByProjectionAsync(projectionName, cancellationToken);
}
