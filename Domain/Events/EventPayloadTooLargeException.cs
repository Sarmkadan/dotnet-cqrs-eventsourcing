#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Exception thrown when an event payload exceeds the configured maximum size.
/// The size is checked before the payload is parsed or deserialized, so an oversized
/// stored or incoming payload cannot force unbounded memory allocation.
/// </summary>
public sealed class EventPayloadTooLargeException : Exception
{
    /// <summary>
    /// Gets the event type name of the oversized payload.
    /// </summary>
    public string EventTypeName { get; }

    /// <summary>
    /// Gets the size of the offending payload in UTF-8 bytes.
    /// </summary>
    public int PayloadBytes { get; }

    /// <summary>
    /// Gets the maximum payload size, in UTF-8 bytes, that was permitted.
    /// </summary>
    public int MaxPayloadBytes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventPayloadTooLargeException"/> class.
    /// </summary>
    /// <param name="eventTypeName">The event type name of the oversized payload.</param>
    /// <param name="payloadBytes">The size of the payload in UTF-8 bytes.</param>
    /// <param name="maxPayloadBytes">The maximum permitted payload size in UTF-8 bytes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventTypeName"/> is <see langword="null"/>.</exception>
    public EventPayloadTooLargeException(string eventTypeName, int payloadBytes, int maxPayloadBytes)
        : base($"Payload of event '{eventTypeName ?? throw new ArgumentNullException(nameof(eventTypeName))}' is {payloadBytes} bytes, " +
               $"which exceeds the maximum of {maxPayloadBytes} bytes.")
    {
        EventTypeName = eventTypeName;
        PayloadBytes = payloadBytes;
        MaxPayloadBytes = maxPayloadBytes;
    }
}
