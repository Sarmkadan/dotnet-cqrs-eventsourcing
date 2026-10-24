#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Nodes;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Events;

namespace DotNetCqrsEventSourcing.Application.Upcasting;

/// <summary>
/// Brings stored event payloads up to the current schema version of their event type.
/// <para>
/// Upcasters are indexed by <c>(eventType, fromVersion)</c>. When an envelope is read, the pipeline
/// looks up the upcaster for each step from the envelope's <see cref="EventEnvelope.SchemaVersion"/>
/// up to the version declared by <see cref="EventVersionAttribute"/> on the resolved CLR type, and applies
/// them in order. Envelopes already at the current version are returned untouched.
/// </para>
/// </summary>
public sealed class EventUpcastingPipeline
{
    private readonly EventTypeRegistry _registry;
    private readonly IReadOnlyDictionary<(string EventType, int FromVersion), IEventUpcaster> _upcasters;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventUpcastingPipeline"/> class.
    /// </summary>
    /// <param name="registry">The registry used to resolve event names to their current CLR types and schema versions.</param>
    /// <param name="upcasters">The upcasters to register. Each <c>(EventType, FromVersion)</c> pair may appear only once.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> or <paramref name="upcasters"/> is <see langword="null"/>, or when an upcaster in the sequence is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when an upcaster has a null or whitespace <see cref="IEventUpcaster.EventType"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an upcaster has a <see cref="IEventUpcaster.FromVersion"/> less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when two upcasters are registered for the same <c>(EventType, FromVersion)</c>.</exception>
    public EventUpcastingPipeline(EventTypeRegistry registry, IEnumerable<IEventUpcaster> upcasters)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(upcasters);

        _registry = registry;

        var map = new Dictionary<(string EventType, int FromVersion), IEventUpcaster>();
        foreach (var upcaster in upcasters)
        {
            ArgumentNullException.ThrowIfNull(upcaster);
            ArgumentException.ThrowIfNullOrWhiteSpace(upcaster.EventType);
            ArgumentOutOfRangeException.ThrowIfLessThan(upcaster.FromVersion, EventVersionAttribute.DefaultVersion);

            if (!map.TryAdd((upcaster.EventType, upcaster.FromVersion), upcaster))
            {
                throw new InvalidOperationException(
                    $"An upcaster is already registered for event '{upcaster.EventType}' from version {upcaster.FromVersion}.");
            }
        }

        _upcasters = map;
    }

    /// <summary>
    /// Returns the payload of <paramref name="envelope"/> converted to the current schema version of its event type.
    /// </summary>
    /// <param name="envelope">The envelope whose <see cref="EventEnvelope.EventData"/> should be upgraded.</param>
    /// <returns>A JSON string that deserializes into the current CLR type of the event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <see langword="null"/>.</exception>
    /// <exception cref="UnknownEventTypeException">Thrown when <see cref="EventEnvelope.EventType"/> is not registered in the <see cref="EventTypeRegistry"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the envelope was written with a schema version newer than the current one, when the payload is not a JSON object, or when no upcaster is registered for one of the required steps.</exception>
    /// <exception cref="System.Text.Json.JsonException">Thrown when <see cref="EventEnvelope.EventData"/> is not valid JSON.</exception>
    public string UpcastToCurrentVersion(EventEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var targetVersion = EventVersionAttribute.GetVersion(_registry.Resolve(envelope.EventType));
        var version = envelope.SchemaVersion;

        if (version == targetVersion)
        {
            return envelope.EventData;
        }

        if (version > targetVersion)
        {
            throw new InvalidOperationException(
                $"Event '{envelope.EventType}' was written with schema version {version}, " +
                $"but this build only understands up to version {targetVersion}.");
        }

        var payload = JsonNode.Parse(envelope.EventData) as JsonObject
            ?? throw new InvalidOperationException(
                $"Payload of event '{envelope.EventType}' is not a JSON object and cannot be upcast.");

        for (; version < targetVersion; version++)
        {
            if (!_upcasters.TryGetValue((envelope.EventType, version), out var upcaster))
            {
                throw new InvalidOperationException(
                    $"No upcaster registered for event '{envelope.EventType}' from version {version} to {version + 1}.");
            }

            payload = upcaster.Upcast(payload);
        }

        return payload.ToJsonString();
    }
}
