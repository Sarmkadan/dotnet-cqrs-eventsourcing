#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Health;

/// <summary>
/// Builds a <see cref="ComponentHealth"/> for a background worker from its liveness signals.
/// Shared by every worker so that all of them apply the same rules:
/// not running or stale heartbeat is <see cref="HealthState.Unhealthy"/>;
/// paused or failing cycles is <see cref="HealthState.Degraded"/>.
/// </summary>
public static class WorkerLiveness
{
    /// <summary>
    /// Evaluates the liveness signals of a worker and describes them as a component.
    /// </summary>
    /// <param name="name">Component identifier of the worker.</param>
    /// <param name="isRunning">Whether the worker's execution loop is currently active.</param>
    /// <param name="isPaused">Whether the worker has been paused by an operator.</param>
    /// <param name="lastHeartbeatUtc">UTC time of the last loop heartbeat, or <see langword="null"/> if none was recorded.</param>
    /// <param name="staleAfter">Maximum age of the heartbeat before the worker is considered stuck.</param>
    /// <param name="consecutiveErrors">Number of cycles that failed since the last successful cycle.</param>
    /// <param name="nowUtc">UTC time used to measure the heartbeat age.</param>
    /// <returns>The worker's health, including <c>running</c>, <c>paused</c>, <c>consecutive_errors</c> and <c>heartbeat_age_ms</c> metrics.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/>, empty, or consists only of white-space.</exception>
    public static ComponentHealth Describe(
        string name,
        bool isRunning,
        bool isPaused,
        DateTime? lastHeartbeatUtc,
        TimeSpan staleAfter,
        int consecutiveErrors,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var heartbeatAge = lastHeartbeatUtc is { } heartbeat ? nowUtc - heartbeat : (TimeSpan?)null;

        var metrics = new Dictionary<string, long>
        {
            ["running"] = isRunning ? 1 : 0,
            ["paused"] = isPaused ? 1 : 0,
            ["consecutive_errors"] = consecutiveErrors,
            ["heartbeat_age_ms"] = heartbeatAge is { } ageForMetric ? (long)ageForMetric.TotalMilliseconds : -1
        };

        var (state, description) = (isRunning, isPaused, consecutiveErrors, heartbeatAge) switch
        {
            (false, _, _, _) => (HealthState.Unhealthy, "Worker is not running."),
            (_, _, _, null) => (HealthState.Unhealthy, "Worker is running but has not reported a heartbeat."),
            (_, _, _, { } age) when age > staleAfter => (HealthState.Unhealthy, $"Heartbeat is stale ({age.TotalSeconds:F0}s old)."),
            (_, true, _, _) => (HealthState.Degraded, "Worker is paused."),
            (_, _, > 0, _) => (HealthState.Degraded, $"{consecutiveErrors} consecutive cycle error(s)."),
            _ => (HealthState.Healthy, "Worker is running.")
        };

        return new ComponentHealth(name, state, description, metrics);
    }
}
