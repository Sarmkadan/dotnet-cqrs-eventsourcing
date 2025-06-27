#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Decorators;

using System.Globalization;
using DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Validation helpers for <see cref="LoggingDecorator"/> to ensure method arguments are valid before logging operations.
/// </summary>
public static class LoggingDecoratorValidation
{
    /// <summary>
    /// Validates a <see cref="LoggingDecorator"/> instance and returns any validation problems.
    /// </summary>
    /// <param name="value">The logging decorator instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(this LoggingDecorator? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Array.Empty<string>();
    }

    /// <summary>
    /// Validates arguments for <see cref="LoggingDecorator.LogEventPublished(DomainEvent)"/> and returns any validation problems.
    /// </summary>
    /// <param name="event">The domain event to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(this DomainEvent? @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(@event.GetEventType()))
        {
            problems.Add("Event type cannot be null, empty, or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(@event.AggregateId))
        {
            problems.Add("AggregateId cannot be null, empty, or whitespace.");
        }

        if (@event.AggregateVersion < 0)
        {
            problems.Add("AggregateVersion cannot be negative.");
        }

        if (@event.OccurredAt == default)
        {
            problems.Add("OccurredAt cannot be the default DateTime value.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates arguments for <see cref="LoggingDecorator.LogEventProcessed(DomainEvent, long)"/> and returns any validation problems.
    /// </summary>
    /// <param name="event">The domain event to validate.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(this DomainEvent? @event, long elapsedMilliseconds)
    {
        var problems = new List<string>(Validate(@event));

        if (elapsedMilliseconds < 0)
        {
            problems.Add("Elapsed milliseconds cannot be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates arguments for <see cref="LoggingDecorator.LogEventProcessingError(DomainEvent, Exception, long)"/> and returns any validation problems.
    /// </summary>
    /// <param name="event">The domain event to validate.</param>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(this DomainEvent? @event, Exception? ex, long elapsedMilliseconds)
    {
        var problems = new List<string>(Validate(@event, elapsedMilliseconds));

        if (ex is null)
        {
            problems.Add("Exception cannot be null.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates arguments for <see cref="LoggingDecorator.LogAggregateOperation(string, string, string, string?)"/> and returns any validation problems.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="aggregateType">The type of the aggregate.</param>
    /// <param name="correlationId">The optional correlation identifier.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(
        string? operationName,
        string? aggregateId,
        string? aggregateType,
        string? correlationId = null)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(operationName))
        {
            problems.Add("Operation name cannot be null, empty, or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            problems.Add("AggregateId cannot be null, empty, or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(aggregateType))
        {
            problems.Add("Aggregate type cannot be null, empty, or whitespace.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates arguments for <see cref="LoggingDecorator.LogConcurrencyConflict(string, long, long)"/> and returns any validation problems.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(
        string? aggregateId,
        long expectedVersion,
        long actualVersion)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            problems.Add("AggregateId cannot be null, empty, or whitespace.");
        }

        if (expectedVersion < 0)
        {
            problems.Add("Expected version cannot be negative.");
        }

        if (actualVersion < 0)
        {
            problems.Add("Actual version cannot be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates arguments for <see cref="LoggingDecorator.LogSnapshotCreated(string, long)"/> and returns any validation problems.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="version">The version at which the snapshot was created.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(string? aggregateId, long version)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            problems.Add("AggregateId cannot be null, empty, or whitespace.");
        }

        if (version < 0)
        {
            problems.Add("Version cannot be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates arguments for <see cref="LoggingDecorator.LogProjectionRebuilt(string, int, long)"/> and returns any validation problems.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="eventCount">The number of events processed.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    public static IReadOnlyList<string> Validate(string? aggregateId, int eventCount, long elapsedMilliseconds)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            problems.Add("AggregateId cannot be null, empty, or whitespace.");
        }

        if (eventCount < 0)
        {
            problems.Add("Event count cannot be negative.");
        }

        if (elapsedMilliseconds < 0)
        {
            problems.Add("Elapsed milliseconds cannot be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="LoggingDecorator"/> instance is valid.
    /// </summary>
    /// <param name="value">The logging decorator instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this LoggingDecorator? value)
    {
        try
        {
            value?.EnsureValid();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that the specified <see cref="LoggingDecorator"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The logging decorator instance to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid.</exception>
    public static void EnsureValid(this LoggingDecorator? value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    /// <summary>
    /// Ensures that the specified <see cref="DomainEvent"/> is valid for <see cref="LoggingDecorator.LogEventPublished(DomainEvent)"/>, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="event">The domain event to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the event is not valid.</exception>
    public static void EnsureValid(this DomainEvent? @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var problems = Validate(@event);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", problems),
                nameof(@event)
            );
        }
    }

    /// <summary>
    /// Ensures that the specified arguments are valid for <see cref="LoggingDecorator.LogEventProcessed(DomainEvent, long)"/>, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="event">The domain event to validate.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <exception cref="ArgumentException">Thrown if the arguments are not valid.</exception>
    public static void EnsureValid(this DomainEvent? @event, long elapsedMilliseconds)
    {
        var problems = Validate(@event, elapsedMilliseconds);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", problems),
                nameof(@event)
            );
        }
    }

    /// <summary>
    /// Ensures that the specified arguments are valid for <see cref="LoggingDecorator.LogEventProcessingError(DomainEvent, Exception, long)"/>, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="event">The domain event to validate.</param>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <exception cref="ArgumentException">Thrown if the arguments are not valid.</exception>
    public static void EnsureValid(this DomainEvent? @event, Exception? ex, long elapsedMilliseconds)
    {
        var problems = Validate(@event, ex, elapsedMilliseconds);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", problems),
                nameof(@event)
            );
        }
    }

    /// <summary>
    /// Ensures that the specified arguments are valid for <see cref="LoggingDecorator.LogAggregateOperation(string, string, string, string?)"/>, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="aggregateType">The type of the aggregate.</param>
    /// <param name="correlationId">The optional correlation identifier.</param>
    /// <exception cref="ArgumentException">Thrown if the arguments are not valid.</exception>
    public static void EnsureValid(
        string? operationName,
        string? aggregateId,
        string? aggregateType,
        string? correlationId = null)
    {
        var problems = Validate(operationName, aggregateId, aggregateType, correlationId);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", problems),
                nameof(operationName)
            );
        }
    }

    /// <summary>
    /// Ensures that the specified arguments are valid for <see cref="LoggingDecorator.LogConcurrencyConflict(string, long, long)"/>, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version.</param>
    /// <exception cref="ArgumentException">Thrown if the arguments are not valid.</exception>
    public static void EnsureValid(
        string? aggregateId,
        long expectedVersion,
        long actualVersion)
    {
        var problems = Validate(aggregateId, expectedVersion, actualVersion);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", problems),
                nameof(aggregateId)
            );
        }
    }

    /// <summary>
    /// Ensures that the specified arguments are valid for <see cref="LoggingDecorator.LogSnapshotCreated(string, long)"/>, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="version">The version at which the snapshot was created.</param>
    /// <exception cref="ArgumentException">Thrown if the arguments are not valid.</exception>
    public static void EnsureValid(string? aggregateId, long version)
    {
        var problems = Validate(aggregateId, version);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", problems),
                nameof(aggregateId)
            );
        }
    }

    /// <summary>
    /// Ensures that the specified arguments are valid for <see cref="LoggingDecorator.LogProjectionRebuilt(string, int, long)"/>, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="eventCount">The number of events processed.</param>
    /// <param name="elapsedMilliseconds">The elapsed time in milliseconds.</param>
    /// <exception cref="ArgumentException">Thrown if the arguments are not valid.</exception>
    public static void EnsureValid(string? aggregateId, int eventCount, long elapsedMilliseconds)
    {
        var problems = Validate(aggregateId, eventCount, elapsedMilliseconds);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", problems),
                nameof(aggregateId)
            );
        }
    }
}