#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Extension methods for <see cref="DomainEvent"/> providing common utility operations
/// for event serialization, metadata manipulation, and event comparison.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Serializes the domain event to a JSON string with camelCase property naming.
    /// </summary>
    /// <param name="domainEvent">The domain event to serialize.</param>
    /// <returns>A JSON string representation of the event.</returns>
    public static string ToJson(this DomainEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        return JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Serializes the domain event to a JSON string with camelCase property naming and indentation.
    /// </summary>
    /// <param name="domainEvent">The domain event to serialize.</param>
    /// <returns>A pretty-printed JSON string representation of the event.</returns>
    public static string ToJsonPretty(this DomainEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        return JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    /// <summary>
    /// Adds or updates a metadata entry with the specified key and value.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The domain event instance for method chaining.</returns>
    public static DomainEvent WithMetadata(this DomainEvent domainEvent, string key, object value)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be null or whitespace.", nameof(key));

        domainEvent.Metadata[key] = value;
        return domainEvent;
    }

    /// <summary>
    /// Creates a deep copy of the domain event, including all metadata.
    /// </summary>
    /// <param name="domainEvent">The domain event to clone.</param>
    /// <returns>A new instance with the same property values and metadata.</returns>
    public static DomainEvent Clone(this DomainEvent domainEvent)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        // Use JSON serialization for deep cloning
        var json = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        return JsonSerializer.Deserialize<DomainEvent>(json) ??
            throw new InvalidOperationException("Failed to deserialize cloned domain event.");
    }
}