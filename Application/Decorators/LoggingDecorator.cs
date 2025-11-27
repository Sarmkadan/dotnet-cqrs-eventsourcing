// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Decorators;

using Microsoft.Extensions.Logging;
using Domain.Events;

/// <summary>
/// Logging decorator for event processing with correlation IDs and performance metrics.
/// </summary>
public class LoggingDecorator
{
    private readonly ILogger<LoggingDecorator> _logger;

    public LoggingDecorator(ILogger<LoggingDecorator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log event publication with correlation tracking.
    /// </summary>
    public void LogEventPublished(DomainEvent @event)
    {
        _logger.LogInformation(
            "Event published: {EventType} | AggregateId: {AggregateId} | Version: {Version} | CorrelationId: {CorrelationId} | Timestamp: {Timestamp}",
            @event.GetEventType(),
            @event.AggregateId,
            @event.AggregateVersion,
            @event.CorrelationId ?? "N/A",
            @event.OccurredAt
        );
    }

    /// <summary>
    /// Log event processing with timing.
    /// </summary>
    public void LogEventProcessed(DomainEvent @event, long elapsedMilliseconds)
    {
        _logger.LogInformation(
            "Event processed: {EventType} | AggregateId: {AggregateId} | Duration: {Duration}ms",
            @event.GetEventType(),
            @event.AggregateId,
            elapsedMilliseconds
        );
    }

    /// <summary>
    /// Log event processing error.
    /// </summary>
    public void LogEventProcessingError(DomainEvent @event, Exception ex, long elapsedMilliseconds)
    {
        _logger.LogError(
            ex,
            "Event processing failed: {EventType} | AggregateId: {AggregateId} | Duration: {Duration}ms | Error: {Error}",
            @event.GetEventType(),
            @event.AggregateId,
            elapsedMilliseconds,
            ex.Message
        );
    }

    /// <summary>
    /// Log aggregate operation with context.
    /// </summary>
    public void LogAggregateOperation(string operationName, string aggregateId, string aggregateType, string? correlationId = null)
    {
        _logger.LogInformation(
            "Aggregate operation: {Operation} | AggregateId: {AggregateId} | AggregateType: {AggregateType} | CorrelationId: {CorrelationId}",
            operationName,
            aggregateId,
            aggregateType,
            correlationId ?? "N/A"
        );
    }

    /// <summary>
    /// Log concurrency conflict.
    /// </summary>
    public void LogConcurrencyConflict(string aggregateId, long expectedVersion, long actualVersion)
    {
        _logger.LogWarning(
            "Concurrency conflict detected: AggregateId: {AggregateId} | Expected Version: {Expected} | Actual Version: {Actual}",
            aggregateId,
            expectedVersion,
            actualVersion
        );
    }

    /// <summary>
    /// Log snapshot creation.
    /// </summary>
    public void LogSnapshotCreated(string aggregateId, long version)
    {
        _logger.LogInformation(
            "Snapshot created: AggregateId: {AggregateId} | Version: {Version}",
            aggregateId,
            version
        );
    }

    /// <summary>
    /// Log projection rebuild.
    /// </summary>
    public void LogProjectionRebuilt(string aggregateId, int eventCount, long elapsedMilliseconds)
    {
        _logger.LogInformation(
            "Projection rebuilt: AggregateId: {AggregateId} | Events: {EventCount} | Duration: {Duration}ms",
            aggregateId,
            eventCount,
            elapsedMilliseconds
        );
    }
}

/// <summary>
/// Performance tracking decorator for measuring operation duration.
/// </summary>
public class PerformanceDecorator
{
    private readonly ILogger<PerformanceDecorator> _logger;
    private const long ThresholdMilliseconds = 1000;

    public PerformanceDecorator(ILogger<PerformanceDecorator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Track and log operation performance.
    /// </summary>
    public void TrackOperation(string operationName, long elapsedMilliseconds)
    {
        if (elapsedMilliseconds > ThresholdMilliseconds)
        {
            _logger.LogWarning(
                "Slow operation detected: {Operation} took {Duration}ms (threshold: {Threshold}ms)",
                operationName,
                elapsedMilliseconds,
                ThresholdMilliseconds
            );
        }
        else
        {
            _logger.LogDebug(
                "Operation completed: {Operation} in {Duration}ms",
                operationName,
                elapsedMilliseconds
            );
        }
    }

    /// <summary>
    /// Get performance summary.
    /// </summary>
    public string GetPerformanceSummary(Dictionary<string, long> operations)
    {
        if (operations.Count == 0)
            return "No operations recorded";

        var totalMs = operations.Values.Sum();
        var avgMs = operations.Values.Average();
        var maxMs = operations.Values.Max();

        return $"Operations: {operations.Count} | Total: {totalMs}ms | Avg: {avgMs:F2}ms | Max: {maxMs}ms";
    }
}
