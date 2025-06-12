#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Handlers;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// System.Text.Json serialization extensions for EventHandlers.
/// </summary>
public static class EventHandlersJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes EventHandlers to JSON string.
    /// </summary>
    /// <param name="value">The EventHandlers instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>JSON string representation of the EventHandlers.</returns>
    public static string ToJson(this EventHandlers value, bool indented = false)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes EventHandlers from JSON string.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <returns>Deserialized EventHandlers instance, or null if JSON is null or empty.</returns>
    public static EventHandlers? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<EventHandlers>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize EventHandlers from JSON string.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <param name="value">Output parameter for the deserialized EventHandlers.</param>
    /// <returns>True if deserialization succeeded; false if JSON is null/empty or if a JsonException occurred.</returns>
    public static bool TryFromJson(string json, out EventHandlers? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<EventHandlers>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}