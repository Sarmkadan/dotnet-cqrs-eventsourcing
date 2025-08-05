#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides System.Text.Json serialization extensions for EventStore.
/// </summary>
public static class EventStoreJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes the EventStore instance to a JSON string.
    /// </summary>
    /// <param name="value">The EventStore instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    /// <returns>A JSON string representation of the EventStore.</returns>
    public static string ToJson(this EventStore value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to an EventStore instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="json"/> is empty or whitespace</exception>
    /// <exception cref="JsonException">Thrown when JSON deserialization fails</exception>
    /// <returns>An EventStore instance.</returns>
    public static EventStore FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<EventStore>(json, _jsonSerializerOptions)
            ?? throw new JsonException("Deserialization returned null");
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an EventStore instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The resulting EventStore instance, or null if deserialization fails.</param>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="json"/> is empty or whitespace</exception>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    public static bool TryFromJson(string json, out EventStore? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<EventStore>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
