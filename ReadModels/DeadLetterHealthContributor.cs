#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Infrastructure.Health;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Reports the unresolved depth of the dead-letter store: the total number of entries awaiting replay
/// and a per-projection breakdown recorded as <c>depth.&lt;projectionName&gt;</c> metrics.
/// The store is degraded whenever at least one entry is unresolved.
/// </summary>
public sealed class DeadLetterHealthContributor : IHealthContributor
{
    /// <summary>Component identifier reported for the dead-letter store.</summary>
    public const string ComponentName = "dead-letter-store";

    private readonly IDeadLetterStore _deadLetterStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterHealthContributor"/> class.
    /// </summary>
    /// <param name="deadLetterStore">Store whose unresolved entries are counted.</param>
    /// <exception cref="ArgumentNullException"><paramref name="deadLetterStore"/> is <see langword="null"/>.</exception>
    public DeadLetterHealthContributor(IDeadLetterStore deadLetterStore)
    {
        ArgumentNullException.ThrowIfNull(deadLetterStore);

        _deadLetterStore = deadLetterStore;
    }

    /// <summary>
    /// Counts the unresolved dead-letter entries, overall and per projection.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the store queries.</param>
    /// <returns>A single <c>dead-letter-store</c> component.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public async Task<IReadOnlyList<ComponentHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var depth = await _deadLetterStore.GetCountAsync(cancellationToken);
        var entries = await _deadLetterStore.GetAllAsync(includeReprocessed: false, cancellationToken);

        var metrics = new Dictionary<string, long> { ["depth"] = depth };
        foreach (var group in entries.GroupBy(entry => entry.ProjectionName, StringComparer.Ordinal))
        {
            metrics[$"depth.{group.Key}"] = group.Count();
        }

        var (state, description) = depth switch
        {
            0 => (HealthState.Healthy, "No unresolved dead-letter entries."),
            _ => (HealthState.Degraded, $"{depth} unresolved dead-letter entr{(depth == 1 ? "y" : "ies")} awaiting replay.")
        };

        return [new ComponentHealth(ComponentName, state, description, metrics)];
    }
}
