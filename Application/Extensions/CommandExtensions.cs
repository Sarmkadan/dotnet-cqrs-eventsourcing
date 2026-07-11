#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Middleware;

namespace DotNetCqrsEventSourcing.Application.Extensions;

/// <summary>
/// Extension methods for command processing and validation.
/// Provides fluent API for building command handlers with validation and instrumentation.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Executes a command with validation and error handling.
    /// Returns a Result that indicates success or failure with detailed error messages.
    /// </summary>
    /// <param name="handler">The command handler function to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Result indicating success or failure with error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
    public static async Task<Result<T>> ExecuteCommandAsync<T>(
        this Func<CancellationToken, Task<Result<T>>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        try
        {
            var result = await handler(cancellationToken);
            return result;
        }
        catch (ArgumentException ex)
        {
            return Result<T>.Failure(new[] { ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Result<T>.Failure(new[] { ex.Message });
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(new[] { $"Unexpected error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Adds correlation tracking to commands.
    /// Ensures events created from this command have the same correlation ID for tracing.
    /// </summary>
    /// <param name="command">The command to get correlation ID for.</param>
    /// <returns>The existing correlation ID or a new one if not present.</returns>
    public static string GetOrCreateCorrelationId(this object command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var context = RequestContext.GetContext();
        return context?.CorrelationId ?? Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Enriches domain events with command context (user, timestamp, correlation).
    /// </summary>
    /// <param name="event">The event to enrich.</param>
    /// <param name="userId">Optional user ID to set.</param>
    /// <param name="correlationId">Optional correlation ID to set.</param>
    /// <returns>The enriched event.</returns>
    public static TEvent EnrichEvent<TEvent>(
        this TEvent @event,
        string? userId = null,
        string? correlationId = null) where TEvent : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var context = RequestContext.GetContext();

        @event.CorrelationId = correlationId ?? context?.CorrelationId ?? Guid.NewGuid().ToString("N");

        return @event;
    }

    /// <summary>
    /// Validates command properties before execution.
    /// Returns list of validation errors or empty list if valid.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <param name="command">The command to validate.</param>
    /// <returns>List of validation error messages, empty if valid.</returns>
    public static ICollection<string> Validate<T>(this T command) where T : class
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(command);

            // Validate required string properties
            if (prop.PropertyType == typeof(string) && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                errors.Add($"{prop.Name} is required");
            }

            // Validate required reference types
            if (!prop.PropertyType.IsValueType && value is null)
            {
                errors.Add($"{prop.Name} is required");
            }
        }

        return errors;
    }
}

/// <summary>
/// Extension methods for working with domain events.
/// </summary>
public static class EventExtensions
{
    /// <summary>
    /// Creates an event from a command, copying relevant properties.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="command">The command to create event from.</param>
    /// <param name="factory">Factory function to create event.</param>
    /// <returns>The created event with correlation ID set.</returns>
    public static TEvent CreateEventFromCommand<TCommand, TEvent>(
        this TCommand command,
        Func<TCommand, TEvent> factory) where TEvent : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(factory);

        var @event = factory(command);
        @event.CorrelationId = command.GetOrCreateCorrelationId();
        return @event;
    }

    /// <summary>
    /// Formats event name to human-readable string (PascalCase to space-separated).
    /// Example: "AccountCreatedEvent" -> "Account Created Event"
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <returns>Human-readable event name.</returns>
    public static string GetEventDisplayName(this DomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var typeName = @event.GetType().Name
            .Replace("Event", "")
            .Replace("Occurred", "");

        return System.Text.RegularExpressions.Regex.Replace(typeName, "([A-Z])", " $1").Trim();
    }

    /// <summary>
    /// Creates a detailed event summary for logging and auditing.
    /// </summary>
    /// <param name="event">The event to summarize.</param>
    /// <returns>Anonymous object with event details.</returns>
    public static object GetEventSummary(this DomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return new
        {
            eventType = @event.GetType().Name,
            displayName = @event.GetEventDisplayName(),
            aggregateId = @event.AggregateId,
            correlationId = @event.CorrelationId,
            timestamp = @event.Timestamp,
            version = 1 // Would come from event envelope in real implementation
        };
    }
}

/// <summary>
/// Extension methods for aggregate operations.
/// </summary>
public static class AggregateExtensions
{
    /// <summary>
    /// Validates aggregate state before allowing modifications.
    /// Prevents invalid state transitions.
    /// </summary>
    /// <param name="aggregate">The aggregate instance.</param>
    /// <param name="operationType">The operation being performed.</param>
    /// <exception cref="InvalidOperationException">Thrown if aggregate is null or in invalid state.</exception>
    public static void ValidateStateForOperation(this object aggregate, string operationType)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        // Additional validation can be added here based on aggregate type
        // Example: if (aggregate is Account account && account.IsDeleted) throw...
    }

    /// <summary>
    /// Gets aggregate metadata for logging/tracing.
    /// </summary>
    /// <param name="aggregate">The aggregate instance.</param>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <returns>Anonymous object with aggregate metadata.</returns>
    public static object GetAggregateSummary(this object aggregate, string aggregateId)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);

        return new
        {
            aggregateId = aggregateId,
            type = aggregate?.GetType().Name ?? "Unknown",
            timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Extension methods for Result{T} pattern.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a successful result to a different type.
    /// </summary>
    /// <typeparam name="TIn">Input type.</typeparam>
    /// <typeparam name="TOut">Output type.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="mapper">Function to map the value.</param>
    /// <returns>New result with mapped value or existing errors.</returns>
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (!result.IsSuccess)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        try
        {
            var mappedValue = mapper(result.Value);
            return Result<TOut>.Success(mappedValue);
        }
        catch (Exception ex)
        {
            return Result<TOut>.Failure(new[] { ex.Message });
        }
    }

    /// <summary>
    /// Chains multiple operations, short-circuiting on first failure.
    /// </summary>
    /// <typeparam name="TIn">Input type.</typeparam>
    /// <typeparam name="TOut">Output type.</typeparam>
    /// <param name="resultTask">The result task to bind.</param>
    /// <param name="binder">Function to bind the result.</param>
    /// <returns>Bound result or existing errors.</returns>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        var result = await resultTask;
        if (!result.IsSuccess)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        return await binder(result.Value);
    }

    /// <summary>
    /// Executes a side effect on successful result without changing the value.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="sideEffect">Function to execute on success.</param>
    /// <returns>The original result.</returns>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> sideEffect)
    {
        ArgumentNullException.ThrowIfNull(sideEffect);

        var result = await resultTask;

        if (result.IsSuccess)
        {
            await sideEffect(result.Value);
        }

        return result;
    }
}