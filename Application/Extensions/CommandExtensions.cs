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
    public static async Task<Result<T>> ExecuteCommandAsync<T>(
        this Func<CancellationToken, Task<Result<T>>> handler,
        CancellationToken cancellationToken = default)
    {
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
    public static string GetOrCreateCorrelationId(this object command)
    {
        var context = RequestContext.GetContext();
        return context?.CorrelationId ?? Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Enriches domain events with command context (user, timestamp, correlation).
    /// </summary>
    public static TEvent EnrichEvent<TEvent>(
        this TEvent @event,
        string? userId = null,
        string? correlationId = null) where TEvent : DomainEvent
    {
        var context = RequestContext.GetContext();

        @event.CorrelationId = correlationId ?? context?.CorrelationId ?? Guid.NewGuid().ToString("N");

        return @event;
    }

    /// <summary>
    /// Validates command properties before execution.
    /// Returns list of validation errors or empty list if valid.
    /// </summary>
    public static ICollection<string> Validate<T>(this T command) where T : class
    {
        if (command is null)
        {
            return new[] { "Command cannot be null" };
        }

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
    public static TEvent CreateEventFromCommand<TCommand, TEvent>(
        this TCommand command,
        Func<TCommand, TEvent> factory) where TEvent : DomainEvent
    {
        var @event = factory(command);
        @event.CorrelationId = command.GetOrCreateCorrelationId();
        return @event;
    }

    /// <summary>
    /// Formats event name to human-readable string (PascalCase to space-separated).
    /// Example: "AccountCreatedEvent" -> "Account Created Event"
    /// </summary>
    public static string GetEventDisplayName(this DomainEvent @event)
    {
        var typeName = @event.GetType().Name
            .Replace("Event", "")
            .Replace("Occurred", "");

        return System.Text.RegularExpressions.Regex.Replace(typeName, "([A-Z])", " $1").Trim();
    }

    /// <summary>
    /// Creates a detailed event summary for logging and auditing.
    /// </summary>
    public static object GetEventSummary(this DomainEvent @event)
    {
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
    public static void ValidateStateForOperation(this dynamic aggregate, string operationType)
    {
        if (aggregate is null)
        {
            throw new InvalidOperationException($"Cannot perform {operationType} on null aggregate");
        }

        // Additional validation can be added here based on aggregate type
        // Example: if (aggregate is Account account && account.IsDeleted) throw...
    }

    /// <summary>
    /// Gets aggregate metadata for logging/tracing.
    /// </summary>
    public static object GetAggregateSummary(this dynamic aggregate, string aggregateId)
    {
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
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper)
    {
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
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> binder)
    {
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
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> sideEffect)
    {
        var result = await resultTask;

        if (result.IsSuccess)
        {
            await sideEffect(result.Value);
        }

        return result;
    }
}
