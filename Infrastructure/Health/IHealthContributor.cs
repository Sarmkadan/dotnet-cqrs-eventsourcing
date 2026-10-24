#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Health;

/// <summary>
/// Implemented by any component that exposes its own health: background workers, projection engines,
/// and stores. Contributors are collected by <see cref="HealthAggregator"/> into one <see cref="HealthSnapshot"/>.
/// </summary>
public interface IHealthContributor
{
    /// <summary>
    /// Reports the current health of every component this contributor owns.
    /// Expected unhealthy conditions are reported as <see cref="HealthState"/> values, not as exceptions.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the probe.</param>
    /// <returns>One <see cref="ComponentHealth"/> entry per component owned by this contributor.</returns>
    Task<IReadOnlyList<ComponentHealth>> GetHealthAsync(CancellationToken cancellationToken = default);
}
