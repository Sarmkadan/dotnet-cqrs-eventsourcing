#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Runtime.Serialization;

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Exception thrown when attempting to deserialize an event with an unknown or unregistered event type.
/// This prevents deserialization gadget vectors by ensuring only explicitly registered event types can be deserialized.
/// </summary>
[Serializable]
public sealed class UnknownEventTypeException : Exception
{
    /// <summary>
    /// The unknown event type name that was attempted to be resolved.
    /// </summary>
    public string EventTypeName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownEventTypeException"/> class.
    /// </summary>
    /// <param name="eventTypeName">The unknown event type name.</param>
    public UnknownEventTypeException(string eventTypeName)
        : base(CreateMessage(eventTypeName))
    {
        EventTypeName = eventTypeName ?? throw new ArgumentNullException(nameof(eventTypeName));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownEventTypeException"/> class with a custom message.
    /// </summary>
    /// <param name="eventTypeName">The unknown event type name.</param>
    /// <param name="message">The custom error message.</param>
    public UnknownEventTypeException(string eventTypeName, string message)
        : base(message)
    {
        EventTypeName = eventTypeName ?? throw new ArgumentNullException(nameof(eventTypeName));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownEventTypeException"/> class with serialization info.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
    private UnknownEventTypeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        EventTypeName = info.GetString(nameof(EventTypeName)) ?? string.Empty;
    }

    /// <summary>
    /// Sets the serialization data.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
    [Obsolete("This method is obsolete and will be removed in a future version.")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(EventTypeName), EventTypeName);
    }

    private static string CreateMessage(string eventTypeName)
    {
        return $"Unknown event type '{eventTypeName}'. Only explicitly registered event types can be deserialized. " +
               "This prevents deserialization gadget vectors and ensures type safety.";
    }
}