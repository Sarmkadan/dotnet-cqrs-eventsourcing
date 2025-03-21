// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// Serialization and deserialization helpers supporting both System.Text.Json and Newtonsoft.Json.
/// Used for event persistence, API responses, and data interchange.
/// Provides consistent JSON formatting and handles common serialization challenges (dates, decimals, nulls).
/// Thread-safe - caches JsonSerializerOptions for reuse.
/// </summary>
public static class SerializationUtilities
{
    // System.Text.Json configuration for modern .NET serialization
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    // Newtonsoft.Json settings for backward compatibility
    private static readonly JsonSerializerSettings NewtonsoftSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatString = "yyyy-MM-ddTHH:mm:ssZ",
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
    };

    /// <summary>
    /// Serializes an object to JSON using System.Text.Json (modern, performant).
    /// Ignores null properties and uses camelCase naming convention.
    /// </summary>
    public static string ToJson<T>(T obj, bool prettyPrint = false)
    {
        try
        {
            var options = prettyPrint ? PrettyJsonOptions : JsonOptions;
            return JsonSerializer.Serialize(obj, options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to serialize object of type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON to an object using System.Text.Json.
    /// Returns null if JSON is null or whitespace.
    /// </summary>
    public static T? FromJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON to type {typeof(T).Name}: {json[..Math.Min(100, json.Length)]}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON with type information. Useful when the concrete type isn't known at compile time.
    /// </summary>
    public static object? FromJson(string json, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, targetType, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON to type {targetType.Name}", ex);
        }
    }

    /// <summary>
    /// Converts an object to a dictionary using reflection and serialization.
    /// Useful for flattening objects for storage or transmission.
    /// </summary>
    public static Dictionary<string, object?> Todictionary<T>(T obj) where T : class
    {
        if (obj is null) return new();

        var json = ToJson(obj);
        using var doc = JsonDocument.Parse(json);

        var result = new Dictionary<string, object?>();
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            result[property.Name] = property.Value.GetRawText();
        }

        return result;
    }

    /// <summary>
    /// Deserializes using Newtonsoft.Json for backward compatibility with older systems.
    /// Prefer System.Text.Json for new code.
    /// </summary>
    public static T? FromJsonNewtonsoft<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(json, NewtonsoftSettings);
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON (Newtonsoft) to type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Clones an object by serializing and deserializing it.
    /// Useful for deep copying objects without implementing ICloneable.
    /// Warning: reflection-based, use sparingly in hot paths.
    /// </summary>
    public static T? DeepClone<T>(T? obj) where T : class
    {
        if (obj is null) return null;

        var json = ToJson(obj);
        return FromJson<T>(json);
    }

    /// <summary>
    /// Merges a JSON patch into an existing object.
    /// Useful for PATCH endpoints that apply partial updates.
    /// </summary>
    public static T? MergeJson<T>(T existing, string patch) where T : class
    {
        if (existing is null) return null;

        var currentJson = ToJson(existing);
        using var currentDoc = JsonDocument.Parse(currentJson);
        using var patchDoc = JsonDocument.Parse(patch);

        var merged = MergeJsonElements(currentDoc.RootElement, patchDoc.RootElement);
        return FromJson<T>(merged.GetRawText());
    }

    /// <summary>
    /// Helper to recursively merge two JSON elements (patch strategy).
    /// </summary>
    private static JsonElement MergeJsonElements(JsonElement current, JsonElement patch)
    {
        if (patch.ValueKind != JsonValueKind.Object)
        {
            return patch;
        }

        var result = new Dictionary<string, JsonElement>();

        foreach (var prop in current.EnumerateObject())
        {
            result[prop.Name] = prop.Value;
        }

        foreach (var prop in patch.EnumerateObject())
        {
            if (result.ContainsKey(prop.Name) && prop.Value.ValueKind == JsonValueKind.Object)
            {
                result[prop.Name] = MergeJsonElements(result[prop.Name], prop.Value);
            }
            else
            {
                result[prop.Name] = prop.Value;
            }
        }

        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        JsonSerializer.Serialize(writer, result, JsonOptions);
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        return JsonDocument.Parse(json).RootElement;
    }
}
