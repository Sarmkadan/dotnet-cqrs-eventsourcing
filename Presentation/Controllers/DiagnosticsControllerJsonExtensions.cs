#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// System.Text.Json serialization extensions for DiagnosticsController to enable
/// JSON serialization/deserialization of controller instances.
/// </summary>
public static class DiagnosticsControllerJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the DiagnosticsController instance to a JSON string.
    /// </summary>
    /// <param name="value">The DiagnosticsController instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the controller.</returns>
    public static string ToJson(this DiagnosticsController value, bool indented = false)
    {
        if (value is null)
        {
            return "{}";
        }

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a DiagnosticsController instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A DiagnosticsController instance, or null if deserialization fails.</returns>
    public static DiagnosticsController? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DiagnosticsController>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a DiagnosticsController instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized DiagnosticsController instance, or null on failure.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out DiagnosticsController? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<DiagnosticsController>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}