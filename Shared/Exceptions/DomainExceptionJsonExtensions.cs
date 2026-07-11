#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Provides extension methods for serializing and deserializing DomainException to/from JSON.
/// </summary>
public static class DomainExceptionJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the DomainException to a JSON string.
    /// </summary>
    /// <param name="value">The DomainException to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the DomainException.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this DomainException value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a DomainException from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized DomainException, or null if the JSON is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static DomainException? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<DomainException>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a DomainException from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized DomainException if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.
    /// Returns true for null or empty JSON strings (consistent with <see cref="FromJson"/> behavior).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out DomainException? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        return string.IsNullOrEmpty(json)
            ? true
            : TryDeserialize(json, out value);

        static bool TryDeserialize(string jsonValue, out DomainException? result)
        {
            try
            {
                result = JsonSerializer.Deserialize<DomainException>(jsonValue, _jsonSerializerOptions);
                return true;
            }
            catch (JsonException)
            {
                result = null;
                return false;
            }
        }
    }
}
