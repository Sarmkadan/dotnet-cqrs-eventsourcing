#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="DotnetCqrsEventsourcingOptions"/>
/// configuration using System.Text.Json.
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
    /// Serializes the <see cref="DotnetCqrsEventsourcingOptions"/> configuration to a JSON string.
    /// </summary>
    /// <param name="options">The options to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the options.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    public static string ToJson(this DotnetCqrsEventsourcingOptions options, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(options);

        var optionsCopy = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            { WriteIndented = true }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(options, optionsCopy);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="DotnetCqrsEventsourcingOptions"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="DotnetCqrsEventsourcingOptions"/> instance, or <see langword="null"/> if the JSON is <see langword="null"/>, empty, or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    public static DotnetCqrsEventsourcingOptions? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DotnetCqrsEventsourcingOptions>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="DotnetCqrsEventsourcingOptions"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">The resulting <see cref="DotnetCqrsEventsourcingOptions"/> instance, or <see langword="null"/> if deserialization fails.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
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