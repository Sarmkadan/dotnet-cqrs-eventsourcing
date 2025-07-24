#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// System.Text.Json serialization extensions for PagedResult types.
/// Provides conversion to/from JSON format for API responses and storage.
/// </summary>
public static class PagedResultJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a PagedResult to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of items in the PagedResult</typeparam>
    /// <param name="value">The PagedResult to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation of the PagedResult</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static string ToJson<T>(this PagedResult<T> value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a PagedResult.
    /// </summary>
    /// <typeparam name="T">The type of items in the PagedResult</typeparam>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized PagedResult</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null</exception>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized</exception>
    public static PagedResult<T> FromJson<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<PagedResult<T>>(json, _jsonOptions)
            ?? throw new JsonException("Deserialization returned null");
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a PagedResult.
    /// </summary>
    /// <typeparam name="T">The type of items in the PagedResult</typeparam>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter containing the deserialized PagedResult or null if deserialization fails</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null</exception>
    public static bool TryFromJson<T>(string json, out PagedResult<T>? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<PagedResult<T>>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}