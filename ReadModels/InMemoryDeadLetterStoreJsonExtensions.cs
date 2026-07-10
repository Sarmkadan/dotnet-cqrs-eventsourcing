#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="InMemoryDeadLetterStore"/>.
/// </summary>
public static class InMemoryDeadLetterStoreJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    /// <summary>
    /// Serializes the <see cref="InMemoryDeadLetterStore"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The dead letter store to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the dead letter store.</returns>
    public static string ToJson(this InMemoryDeadLetterStore value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = indented
        };

        // Note: InMemoryDeadLetterStore contains a ConcurrentDictionary which isn't directly serializable.
        // This method is provided for API consistency but will throw NotSupportedException at runtime.
        // Use the store's methods to serialize individual DeadLetterEntry instances instead.
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="InMemoryDeadLetterStore"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized <see cref="InMemoryDeadLetterStore"/> instance, or null if the JSON is null or empty.</returns>
    public static InMemoryDeadLetterStore? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<InMemoryDeadLetterStore>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="InMemoryDeadLetterStore"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The resulting <see cref="InMemoryDeadLetterStore"/> instance, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    public static bool TryFromJson(string json, out InMemoryDeadLetterStore? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<InMemoryDeadLetterStore>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}