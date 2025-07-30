#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
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
    private static readonly JsonSerializerOptions DefaultOptions = CreateOptions(writeIndented: false, ignoreNulls: true);
    private static readonly JsonSerializerOptions PrettyOptions = CreateOptions(writeIndented: true, ignoreNulls: true);
    private static readonly JsonSerializerOptions DefaultWithNullsOptions = CreateOptions(writeIndented: false, ignoreNulls: false);
    private static readonly JsonSerializerOptions PrettyWithNullsOptions = CreateOptions(writeIndented: true, ignoreNulls: false);

    private static JsonSerializerOptions CreateOptions(bool writeIndented, bool ignoreNulls) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = writeIndented,
        DefaultIgnoreCondition = ignoreNulls ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never,
        Converters =
        {
            new JsonStringEnumConverter(),
            new JsonDateTimeConverter(),
            new JsonDecimalConverter()
        }
    };

    private static JsonSerializerOptions ResolveOptions(JsonFormatOptions? options) => options switch
    {
        null => DefaultOptions,
        { PrettyPrint: true, IgnoreNulls: false } => PrettyWithNullsOptions,
        { PrettyPrint: true } => PrettyOptions,
        { IgnoreNulls: false } => DefaultWithNullsOptions,
        _ => DefaultOptions
    };

    public string Format<T>(T obj, JsonFormatOptions? options = null)
    {
        if (obj is null)
        {
            return "null";
        }

        var opts = ResolveOptions(options);

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

        var opts = ResolveOptions(options);

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

public sealed record JsonFormatOptions
{
    public bool PrettyPrint { get; init; }
    public bool IgnoreNulls { get; init; } = true;
}

/// <summary>
/// Custom JSON converter for DateTime to ensure ISO 8601 format.
/// </summary>
public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Failed to parse DateTime: unexpected token {reader.TokenType}");
        }

        var raw = reader.GetString();

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
        {
            return date;
        }

        throw new JsonException($"Failed to parse DateTime: {raw}");
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
            var raw = reader.GetString();

            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new JsonException($"Failed to parse decimal: {raw}");
        }

        throw new JsonException($"Failed to parse decimal: unexpected token {reader.TokenType}");
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
