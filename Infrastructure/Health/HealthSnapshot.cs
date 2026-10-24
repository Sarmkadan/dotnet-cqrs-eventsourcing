#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Health;

/// <summary>
/// A point-in-time view of every health component reported by the registered contributors.
/// </summary>
/// <param name="CapturedAtUtc">UTC time at which the snapshot was collected.</param>
/// <param name="Components">Health of each component included in the snapshot.</param>
public sealed record HealthSnapshot(
    DateTime CapturedAtUtc,
    IReadOnlyList<ComponentHealth> Components)
{
    /// <summary>
    /// Gets the worst state across all components, or <see cref="HealthState.Healthy"/> when no components were reported.
    /// </summary>
    public HealthState State => Components
        .Select(component => component.State)
        .DefaultIfEmpty(HealthState.Healthy)
        .Max();
}
