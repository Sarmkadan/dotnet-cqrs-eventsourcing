// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetCqrsEventSourcing.Infrastructure.Formatters;

/// <summary>
/// JSON output formatter for consistent, configurable JSON serialization.
/// Handles camelCase property names, null value handling, enum serialization.
/// Provides both compact and pretty-printed output options.
/// Thread-safe and reusable across all API endpoints.
/// </summary>
public interface IJsonFormatter
{
    /// <summary>
    /// Formats an object to JSON string.
    /// </summary>
    string Format<T>(T obj, JsonFormatOptions? options = null);

    /// <summary>
    /// Formats a collection to JSON array.
    /// </summary>
    string FormatCollection<T>(IEnumerable<T> items, JsonFormatOptions? options = null);

    /// <summary>
    /// Parses JSON string to object.
    /// </summary>
    T? Parse<T>(string json);

    /// <summary>
    /// Gets default JSON serializer options used by formatter.
    /// </summary>
    JsonSerializerOptions GetOptions();
}

public class JsonFormatter : IJsonFormatter
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new JsonDateTimeConverter(),
            new JsonDecimalConverter()
        }
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new JsonDateTimeConverter(),
            new JsonDecimalConverter()
        }
    };

    public string Format<T>(T obj, JsonFormatOptions? options = null)
    {
        if (obj is null)
        {
            return "null";
        }

        var opts = options?.PrettyPrint ?? false ? PrettyOptions : DefaultOptions;

        try
        {
            return JsonSerializer.Serialize(obj, opts);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to format object of type {typeof(T).Name}", ex);
        }
    }

    public string FormatCollection<T>(IEnumerable<T> items, JsonFormatOptions? options = null)
    {
        if (items is null)
        {
            return "[]";
        }

        var opts = options?.PrettyPrint ?? false ? PrettyOptions : DefaultOptions;

        try
        {
            return JsonSerializer.Serialize(items.ToList(), opts);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to format collection", ex);
        }
    }

    public T? Parse<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON to type {typeof(T).Name}", ex);
        }
    }

    public JsonSerializerOptions GetOptions() => DefaultOptions;
}

public record JsonFormatOptions
{
    public bool PrettyPrint { get; init; }
    public bool IgnoreNulls { get; init; }
}

/// <summary>
/// Custom JSON converter for DateTime to ensure ISO 8601 format.
/// </summary>
public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (DateTime.TryParse(reader.GetString(), out var date))
        {
            return date;
        }

        throw new JsonException($"Failed to parse DateTime: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O")); // ISO 8601 format
    }
}

/// <summary>
/// Custom JSON converter for decimal to handle precision correctly.
/// </summary>
public class JsonDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return decimal.Parse(reader.GetString() ?? "0");
        }

        throw new JsonException($"Failed to parse decimal: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

/// <summary>
/// Extension methods for registering JSON formatter.
/// </summary>
public static class JsonFormatterExtensions
{
    public static IServiceCollection AddJsonFormatter(this IServiceCollection services)
    {
        services.AddSingleton<IJsonFormatter, JsonFormatter>();
        return services;
    }
}
