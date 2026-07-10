#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// System.Text.Json serialization extensions for DependencyInjection configuration.
/// </summary>
public static class DependencyInjectionJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the DependencyInjection configuration options to a JSON string.
    /// </summary>
    /// <param name="options">The DotnetCqrsEventsourcingOptions to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the options.</returns>
    public static string ToJson(this DotnetCqrsEventsourcingOptions options, bool indented = false)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var optionsCopy = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(options, optionsCopy);
    }

    /// <summary>
    /// Deserializes a JSON string to a DotnetCqrsEventsourcingOptions instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A DotnetCqrsEventsourcingOptions instance, or null if the JSON is null or empty.</returns>
    public static DotnetCqrsEventsourcingOptions? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DotnetCqrsEventsourcingOptions>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a DotnetCqrsEventsourcingOptions instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">The resulting DotnetCqrsEventsourcingOptions instance, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    public static bool TryFromJson(string json, out DotnetCqrsEventsourcingOptions? options)
    {
        options = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            options = JsonSerializer.Deserialize<DotnetCqrsEventsourcingOptions>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}