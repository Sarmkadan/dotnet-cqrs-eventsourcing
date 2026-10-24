#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Declares the current payload schema version of a domain event class.
/// <para>
/// Event classes without this attribute are schema version <see cref="DefaultVersion"/> (1).
/// Bump the version whenever the serialized shape of the event changes incompatibly
/// (renamed, removed or re-typed fields) and register an
/// <c>IEventUpcaster</c> that converts the previous version to the next one, so that
/// streams written with older schemas remain replayable.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EventVersionAttribute : Attribute
{
    /// <summary>
    /// Schema version assumed for event classes that do not declare <see cref="EventVersionAttribute"/>.
    /// </summary>
    public const int DefaultVersion = 1;

    /// <summary>
    /// Gets the schema version of the decorated event class.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventVersionAttribute"/> class.
    /// </summary>
    /// <param name="version">The schema version, starting at 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="version"/> is less than 1.</exception>
    public EventVersionAttribute(int version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, DefaultVersion);

        Version = version;
    }

    /// <summary>
    /// Returns the schema version of an event type, or <see cref="DefaultVersion"/> when the type is not decorated.
    /// </summary>
    /// <param name="eventType">The event class to inspect.</param>
    /// <returns>The declared schema version of <paramref name="eventType"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventType"/> is <see langword="null"/>.</exception>
    public static int GetVersion(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return eventType.GetCustomAttribute<EventVersionAttribute>(inherit: false)?.Version ?? DefaultVersion;
    }
}
