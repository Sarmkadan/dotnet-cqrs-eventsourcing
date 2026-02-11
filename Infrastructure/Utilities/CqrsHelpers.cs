// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Reflection;
using DotNetCqrsEventSourcing.Shared.Exceptions;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// CQRS-specific helper utilities for command/query routing and handler discovery.
/// Provides dynamic handler resolution, command/event type mapping, and aggregation support.
/// Thread-safe caching of handlers and type information for performance.
/// </summary>
public static class CqrsHelpers
{
    private static readonly ConcurrentDictionary<Type, HandlerMetadata> HandlerMetadataCache = new();
    private static readonly ConcurrentDictionary<string, Type> EventTypeMap = new();

    /// <summary>
    /// Gets all command handler types from an assembly.
    /// Discovers classes implementing ICommandHandler{T} or specific command handler interfaces.
    /// </summary>
    public static IEnumerable<Type> GetCommandHandlers(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                   t.GetInterfaces().Any(i => i.Name.StartsWith("ICommandHandler")))
            .ToArray();
    }

    /// <summary>
    /// Gets all event handler types from an assembly.
    /// Discovers classes implementing IEventHandler{T} or event observer patterns.
    /// </summary>
    public static IEnumerable<Type> GetEventHandlers(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                   t.GetInterfaces().Any(i => i.Name.StartsWith("IEventHandler")))
            .ToArray();
    }

    /// <summary>
    /// Gets metadata about a command/query type (name, aggregate type, result type).
    /// Caches results to avoid repeated reflection.
    /// </summary>
    public static HandlerMetadata GetHandlerMetadata(Type commandType)
    {
        return HandlerMetadataCache.GetOrAdd(commandType, type =>
        {
            var displayName = type.Name.Replace("Command", "").Replace("Query", "");
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            return new HandlerMetadata
            {
                CommandType = type,
                DisplayName = displayName,
                Properties = properties
            };
        });
    }

    /// <summary>
    /// Registers an event type by name for later deserialization.
    /// Allows storing event type names in persisted event streams without versioning issues.
    /// </summary>
    public static void RegisterEventType(Type eventType)
    {
        var typeName = eventType.Name;
        EventTypeMap.TryAdd(typeName, eventType);
    }

    /// <summary>
    /// Resolves an event type by name (from persisted event store).
    /// Returns null if event type not registered.
    /// </summary>
    public static Type? ResolveEventType(string eventTypeName)
    {
        return EventTypeMap.TryGetValue(eventTypeName, out var eventType) ? eventType : null;
    }

    /// <summary>
    /// Extracts the aggregate ID from a command or event.
    /// Assumes the command/event has a property ending with "Id" or "AggregateId".
    /// </summary>
    public static string? ExtractAggregateId(object commandOrEvent)
    {
        if (commandOrEvent is null) return null;

        var type = commandOrEvent.GetType();
        var idProperty = type.GetProperty("AggregateId") ??
                        type.GetProperty("Id") ??
                        type.GetProperties().FirstOrDefault(p => p.Name.EndsWith("Id"));

        return idProperty?.GetValue(commandOrEvent)?.ToString();
    }

    /// <summary>
    /// Gets the aggregate type that a command targets.
    /// Parses command name looking for aggregate type or uses explicit AggregateType property.
    /// </summary>
    public static Type? GetTargetAggregateType(Type commandType)
    {
        // First check for explicit AggregateType property
        var aggregateTypeProp = commandType.GetProperty("AggregateType");
        if (aggregateTypeProp is not null)
        {
            return aggregateTypeProp.PropertyType;
        }

        // Try to infer from command name (e.g., "CreateAccountCommand" -> Account)
        var commandName = commandType.Name.Replace("Command", "").Replace("Query", "");
        var targetAssembly = commandType.Assembly;

        return targetAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == commandName || t.Name.StartsWith(commandName));
    }

    /// <summary>
    /// Validates that all required command properties are set (non-null for ref types, non-default for value types).
    /// Returns collection of validation errors or empty list if valid.
    /// </summary>
    public static ICollection<string> ValidateCommand(object command)
    {
        var errors = new List<string>();
        var type = command.GetType();
        var metadata = GetHandlerMetadata(type);

        foreach (var prop in metadata.Properties)
        {
            var value = prop.GetValue(command);

            if (prop.PropertyType.IsValueType && Equals(value, Activator.CreateInstance(prop.PropertyType)))
            {
                errors.Add($"{prop.Name} cannot have default value");
            }
            else if (!prop.PropertyType.IsValueType && value is null)
            {
                errors.Add($"{prop.Name} cannot be null");
            }
        }

        return errors;
    }

    /// <summary>
    /// Clears internal caches. Useful in tests when handler assemblies change.
    /// </summary>
    public static void ClearCaches()
    {
        HandlerMetadataCache.Clear();
        EventTypeMap.Clear();
    }
}

/// <summary>
/// Metadata about a command/query extracted through reflection.
/// Used for validation, routing, and introspection.
/// </summary>
public class HandlerMetadata
{
    public required Type CommandType { get; init; }
    public required string DisplayName { get; init; }
    public PropertyInfo[] Properties { get; init; } = Array.Empty<PropertyInfo>();
}
