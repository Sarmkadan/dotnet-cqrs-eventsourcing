#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Reflection;
using DotNetCqrsEventSourcing.Domain.Events;
using Microsoft.Extensions.Logging;

namespace DotNetCqrsEventSourcing.Infrastructure.Events;

/// <summary>
/// Registry that maps stable event-name strings (as stored in the event store)
/// to their concrete <see cref="DomainEvent"/> types.
/// <para>
/// Rather than relying on <c>Type.GetType()</c> with a fully-qualified class name —
/// which breaks when assemblies are renamed or namespaces refactored — this registry
/// uses the <see cref="EventNameAttribute"/> as the stable identifier.
/// </para>
/// <para>
/// Usage:
/// <list type="bullet">
///   <item><description>
///     Decorate every event class with <c>[EventName("MyEventName")]</c>.
///   </description></item>
///   <item><description>
///     Call <see cref="ScanAssembly"/> once at startup (or add it to DI via
///     <see cref="DotNetCqrsEventSourcing.Configuration.DependencyInjection"/>).
///   </description></item>
///   <item><description>
///     Use <see cref="Resolve"/> inside deserialization instead of <c>Type.GetType()</c>.
///   </description></item>
/// </list>
/// </para>
/// </summary>
public sealed class EventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _registry = new(StringComparer.Ordinal);
    private readonly ILogger<EventTypeRegistry>? _logger;

    public EventTypeRegistry(ILogger<EventTypeRegistry>? logger = null)
    {
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Registration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Explicitly registers <typeparamref name="T"/> under <paramref name="eventName"/>.
    /// </summary>
    /// <typeparam name="T">Concrete domain-event type to register.</typeparam>
    /// <param name="eventName">
    /// Stable event name string.  Must match what is stored in the event envelope's
    /// <c>EventType</c> field.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="eventName"/> is null or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="eventName"/> is already mapped to a different type.
    /// </exception>
    public void Register<T>(string eventName) where T : DomainEvent
        => RegisterInternal(eventName, typeof(T));

    /// <summary>
    /// Scans <paramref name="assembly"/> and registers every concrete
    /// <see cref="DomainEvent"/> subclass that is decorated with
    /// <see cref="EventNameAttribute"/>.
    /// </summary>
    /// <param name="assembly">Assembly to scan.</param>
    public void ScanAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var candidates = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(DomainEvent).IsAssignableFrom(t));

        foreach (var type in candidates)
        {
            var attr = type.GetCustomAttribute<EventNameAttribute>(inherit: false);
            if (attr is null)
                continue;

            RegisterInternal(attr.Name, type);
        }
    }

    // -------------------------------------------------------------------------
    // Resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the <see cref="Type"/> registered under <paramref name="eventName"/>,
    /// or <see langword="null"/> when the name is unknown.
    /// </summary>
    public Type? Resolve(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
            return null;

        _registry.TryGetValue(eventName, out var type);
        return type;
    }

    /// <summary>
    /// Returns <see langword="true"/> and sets <paramref name="type"/> when
    /// <paramref name="eventName"/> is registered; otherwise returns
    /// <see langword="false"/>.
    /// </summary>
    public bool TryResolve(string eventName, out Type? type)
    {
        type = Resolve(eventName);
        return type is not null;
    }

    /// <summary>
    /// Returns a snapshot of all registered event-name → type mappings.
    /// </summary>
    public IReadOnlyDictionary<string, Type> GetAllRegistrations()
        => _registry;

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void RegisterInternal(string eventName, Type type)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name must not be null or whitespace.", nameof(eventName));

        if (_registry.TryGetValue(eventName, out var existing) && existing != type)
        {
            throw new InvalidOperationException(
                $"Event name '{eventName}' is already registered to '{existing.FullName}'. " +
                $"Cannot re-register it to '{type.FullName}'.");
        }

        _registry[eventName] = type;

        _logger?.LogDebug(
            "EventTypeRegistry: registered '{EventName}' → {TypeName}",
            eventName, type.FullName);
    }
}
