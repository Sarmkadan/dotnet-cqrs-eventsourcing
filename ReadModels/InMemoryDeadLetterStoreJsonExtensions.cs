#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="InMemoryDeadLetterStore"/>.
/// </summary>
public static class InMemoryDeadLetterStoreJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="InMemoryDeadLetterStore"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The dead letter store to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the dead letter store entries.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this InMemoryDeadLetterStore value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = indented
        };

        // Extract values from ConcurrentDictionary for serialization
        var entries = value.GetAllAsync(includeReprocessed: true)
            .GetAwaiter()
            .GetResult()
            .ToList();

        return JsonSerializer.Serialize(entries, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a collection of <see cref="DeadLetterEntry"/> instances.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A collection of deserialized <see cref="DeadLetterEntry"/> instances, or an empty collection if the JSON is null or empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<DeadLetterEntry>? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return string.IsNullOrWhiteSpace(json)
            ? Array.Empty<DeadLetterEntry>()
            : JsonSerializer.Deserialize<DeadLetterEntry[]>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a collection of <see cref="DeadLetterEntry"/> instances.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The resulting collection of <see cref="DeadLetterEntry"/> instances, or an empty array if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson(string json, out IReadOnlyList<DeadLetterEntry>? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = string.IsNullOrWhiteSpace(json)
                ? Array.Empty<DeadLetterEntry>()
                : JsonSerializer.Deserialize<DeadLetterEntry[]>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = Array.Empty<DeadLetterEntry>();
            return false;
        }
    }
}