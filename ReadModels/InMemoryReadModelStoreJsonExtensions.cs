#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetCqrsEventSourcing.ReadModels;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="InMemoryReadModelStore{TReadModel}"/>.
/// </summary>
public static class InMemoryReadModelStoreJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="InMemoryReadModelStore{TReadModel}"/> to a JSON string.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type stored in the store.</typeparam>
    /// <param name="value">The read model store to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the read model store.</returns>
    public static string ToJson<TReadModel>(this InMemoryReadModelStore<TReadModel> value, bool indented = false)
        where TReadModel : class
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
    /// Deserializes a JSON string to an <see cref="InMemoryReadModelStore{TReadModel}"/> instance.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type stored in the store.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An instance of <see cref="InMemoryReadModelStore{TReadModel}"/> populated with the deserialized data.</returns>
    public static InMemoryReadModelStore<TReadModel>? FromJson<TReadModel>(string json)
        where TReadModel : class
    {
        return JsonSerializer.Deserialize<InMemoryReadModelStore<TReadModel>>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="InMemoryReadModelStore{TReadModel}"/> instance.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type stored in the store.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The resulting read model store, or null if deserialization failed.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson<TReadModel>(string json, out InMemoryReadModelStore<TReadModel>? value)
        where TReadModel : class
    {
        try
        {
            value = JsonSerializer.Deserialize<InMemoryReadModelStore<TReadModel>>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}