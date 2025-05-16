#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Marks a domain event class with a stable, human-readable event name used by
/// the <see cref="DotNetCqrsEventSourcing.Infrastructure.Events.EventTypeRegistry"/>
/// for serialization and deserialization.
/// Using an explicit name decouples the stored event type string from the
/// C# class name, so renaming or moving a class does not break existing event streams.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EventNameAttribute : Attribute
{
    /// <summary>Stable event name as it appears in the event store.</summary>
    public string Name { get; }

    public EventNameAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Event name must not be null or whitespace.", nameof(name));

        Name = name;
    }
}
