#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Health;

/// <summary>
/// Severity of a single component or of the whole system. Values are ordered from best to worst,
/// so the aggregate state of several components is their maximum.
/// </summary>
public enum HealthState
{
    /// <summary>The component is running and within all of its thresholds.</summary>
    Healthy = 0,

    /// <summary>The component is running, but a threshold is exceeded (paused, recent errors, lag, dead letters).</summary>
    Degraded = 1,

    /// <summary>The component is not running, has stopped reporting progress, or its health probe failed.</summary>
    Unhealthy = 2
}
