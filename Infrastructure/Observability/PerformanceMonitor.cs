// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.Collections.Concurrent;

namespace DotNetCqrsEventSourcing.Infrastructure.Observability;

/// <summary>
/// Performance monitoring and telemetry collection for CQRS operations.
/// Tracks execution times, error rates, and throughput for commands, queries, and domain events.
/// Useful for identifying performance bottlenecks and detecting anomalies.
/// Thread-safe using concurrent collections for production high-throughput scenarios.
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// Records execution of an operation with its duration.
    /// </summary>
    void RecordOperation(string operationName, long durationMs, bool success = true);

    /// <summary>
    /// Gets performance statistics for a specific operation.
    /// </summary>
    OperationStatistics? GetStatistics(string operationName);

    /// <summary>
    /// Gets all recorded operation statistics.
    /// </summary>
    IEnumerable<(string Name, OperationStatistics Stats)> GetAllStatistics();

    /// <summary>
    /// Clears all recorded metrics.
    /// </summary>
    void Clear();
}

public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, OperationMetrics> _metrics = new();
    private readonly ILogger<PerformanceMonitor> _logger;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordOperation(string operationName, long durationMs, bool success = true)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(operationName);

        var metrics = _metrics.GetOrAdd(operationName, _ => new OperationMetrics());

        lock (metrics)
        {
            metrics.InvocationCount++;
            metrics.TotalDurationMs += durationMs;
            metrics.MinDurationMs = Math.Min(metrics.MinDurationMs, durationMs);
            metrics.MaxDurationMs = Math.Max(metrics.MaxDurationMs, durationMs);

            if (!success)
            {
                metrics.FailureCount++;
            }

            metrics.LastInvokedAt = DateTime.UtcNow;

            // Log slow operations
            if (durationMs > 1000)
            {
                _logger.LogWarning(
                    "Slow operation detected: {Operation} took {DurationMs}ms",
                    operationName,
                    durationMs
                );
            }
        }
    }

    public OperationStatistics? GetStatistics(string operationName)
    {
        if (!_metrics.TryGetValue(operationName, out var metrics))
        {
            return null;
        }

        lock (metrics)
        {
            return new OperationStatistics
            {
                OperationName = operationName,
                InvocationCount = metrics.InvocationCount,
                SuccessCount = metrics.InvocationCount - metrics.FailureCount,
                FailureCount = metrics.FailureCount,
                SuccessRate = metrics.InvocationCount > 0 ? (double)(metrics.InvocationCount - metrics.FailureCount) / metrics.InvocationCount : 0,
                AverageDurationMs = metrics.InvocationCount > 0 ? (double)metrics.TotalDurationMs / metrics.InvocationCount : 0,
                MinDurationMs = metrics.MinDurationMs,
                MaxDurationMs = metrics.MaxDurationMs,
                LastInvokedAt = metrics.LastInvokedAt
            };
        }
    }

    public IEnumerable<(string Name, OperationStatistics Stats)> GetAllStatistics()
    {
        foreach (var kvp in _metrics)
        {
            var stats = GetStatistics(kvp.Key);
            if (stats is not null)
            {
                yield return (kvp.Key, stats);
            }
        }
    }

    public void Clear()
    {
        _metrics.Clear();
        _logger.LogInformation("Performance metrics cleared");
    }

    private class OperationMetrics
    {
        public long InvocationCount { get; set; }
        public long FailureCount { get; set; }
        public long TotalDurationMs { get; set; }
        public long MinDurationMs { get; set; } = long.MaxValue;
        public long MaxDurationMs { get; set; }
        public DateTime LastInvokedAt { get; set; }
    }
}

public class OperationStatistics
{
    public string OperationName { get; set; } = string.Empty;
    public long InvocationCount { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public DateTime LastInvokedAt { get; set; }
}

/// <summary>
/// Disposable scope for timing an operation.
/// Automatically records the operation duration on dispose.
/// Usage: using (var scope = monitor.StartOperation("MyOp")) { ... }
/// </summary>
public class PerformanceScope : IDisposable
{
    private readonly IPerformanceMonitor _monitor;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;

    public PerformanceScope(IPerformanceMonitor monitor, string operationName)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _monitor.RecordOperation(_operationName, _stopwatch.ElapsedMilliseconds);
    }
}

/// <summary>
/// Extension method to start a performance scope.
/// </summary>
public static class PerformanceMonitorExtensions
{
    public static PerformanceScope StartOperation(this IPerformanceMonitor monitor, string operationName)
    {
        return new PerformanceScope(monitor, operationName);
    }
}

/// <summary>
/// Health check endpoint for performance metrics.
/// Useful for monitoring application health and detecting performance degradation.
/// </summary>
public class PerformanceHealthCheck : IHealthCheck
{
    private readonly IPerformanceMonitor _monitor;
    private const double FailureRateThreshold = 0.1; // 10% failures triggers warning

    public PerformanceHealthCheck(IPerformanceMonitor monitor)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stats = _monitor.GetAllStatistics().ToList();

        var failingOperations = stats
            .Where(s => (1 - s.Stats.SuccessRate) > FailureRateThreshold)
            .ToList();

        if (failingOperations.Count > 0)
        {
            var description = $"{failingOperations.Count} operations exceeding {FailureRateThreshold:P} failure rate";
            return Task.FromResult(HealthCheckResult.Degraded(description));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Performance metrics nominal"));
    }
}
