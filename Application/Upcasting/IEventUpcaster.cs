#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Nodes;

namespace DotNetCqrsEventSourcing.Application.Upcasting;

/// <summary>
/// Converts the stored payload of one event type from schema version <see cref="FromVersion"/>
/// to schema version <see cref="FromVersion"/> + 1.
/// <para>
/// Upcasters operate on the raw JSON payload, before it is materialized into the current CLR event
/// type, so they can express renames, added or removed fields and type changes that the current
/// class no longer represents. Each upcaster advances exactly one version; the
/// <see cref="EventUpcastingPipeline"/> chains them to reach the version declared by
/// <see cref="Domain.Events.EventVersionAttribute"/>.
/// </para>
/// </summary>
public interface IEventUpcaster
{
    /// <summary>
    /// Gets the stable event name (as used by <see cref="Domain.Events.EventNameAttribute"/>) this upcaster handles.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the schema version this upcaster reads. The output is version <c>FromVersion + 1</c>.
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// Transforms a payload from <see cref="FromVersion"/> into <c>FromVersion + 1</c>.
    /// </summary>
    /// <param name="payload">The stored JSON object of the event. Implementations may modify and return it.</param>
    /// <returns>The payload in the next schema version.</returns>
    JsonObject Upcast(JsonObject payload);
}
