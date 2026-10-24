#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Health;

/// <summary>
/// Health of one named component, such as a background worker, a read-model projection,
/// or the dead-letter store. Metrics are numeric so they can be logged as structured values
/// and compared between snapshots.
/// </summary>
/// <param name="Name">Stable identifier of the component, for example <c>projection:AccountReadModel</c>.</param>
/// <param name="State">Health state of the component.</param>
/// <param name="Description">Short human-readable reason for the state.</param>
/// <param name="Metrics">Numeric metrics keyed by metric name.</param>
public sealed record ComponentHealth(
    string Name,
    HealthState State,
    string Description,
    IReadOnlyDictionary<string, long> Metrics);
