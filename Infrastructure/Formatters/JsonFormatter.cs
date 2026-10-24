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

/// <summary>
/// Default <see cref="IJsonFormatter"/> implementation backed by <see cref="JsonSerializer"/>.
/// </summary>
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

    /// <summary>
    /// Serializes an object to a JSON string using camelCase names and the supplied formatting options.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize; must not be <see langword="null"/>.</param>
    /// <param name="options">Formatting options; when <see langword="null"/> the default compact, null-ignoring options are used.</param>
    /// <returns>The JSON representation of <paramref name="obj"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails.</exception>
    public string Format<T>(T obj, JsonFormatOptions? options = null)
    {
        // Guard clause for reference-type parameter 'obj'
        ArgumentNullException.ThrowIfNull(obj, nameof(obj));

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

    /// <summary>
    /// Serializes a sequence to a JSON array using camelCase names and the supplied formatting options.
    /// </summary>
    /// <typeparam name="T">The element type of the sequence.</typeparam>
    /// <param name="items">The items to serialize; must not be <see langword="null"/>.</param>
    /// <param name="options">Formatting options; when <see langword="null"/> the default compact, null-ignoring options are used.</param>
    /// <returns>The JSON array representation of <paramref name="items"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails.</exception>
    public string FormatCollection<T>(IEnumerable<T> items, JsonFormatOptions? options = null)
    {
        // Guard clause for reference-type parameter 'items'
        ArgumentNullException.ThrowIfNull(items, nameof(items));

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

    /// <summary>
    /// Deserializes a JSON string into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="json">The JSON text to parse.</param>
    /// <returns>The deserialized object, or the default value when <paramref name="json"/> is white space.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the JSON cannot be parsed into <typeparamref name="T"/>.</exception>
    public T? Parse<T>(string json)
    {
        // Guard clause for string parameter 'json'
        ArgumentException.ThrowIfNullOrEmpty(json, nameof(json));

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

    /// <summary>
    /// Gets the default <see cref="JsonSerializerOptions"/> used by this formatter.
    /// </summary>
    /// <returns>The compact serializer options that ignore null values.</returns>
    public JsonSerializerOptions GetOptions() => DefaultOptions;
}

/// <summary>
/// Formatting options for <see cref="IJsonFormatter"/> output.
/// </summary>
public sealed record JsonFormatOptions
{
    /// <summary>Gets a value indicating whether the output is indented for readability.</summary>
    public bool PrettyPrint { get; init; }

    /// <summary>Gets a value indicating whether properties with <see langword="null"/> values are omitted. Defaults to <see langword="true"/>.</summary>
    public bool IgnoreNulls { get; init; } = true;
}

/// <summary>
/// Custom JSON converter for DateTime to ensure ISO 8601 format.
/// </summary>
public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// Reads an ISO 8601 string token and converts it to a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to read.</param>
    /// <param name="typeToConvert">The type to convert to.</param>
    /// <param name="options">The serializer options in use.</param>
    /// <returns>The parsed <see cref="DateTime"/>.</returns>
    /// <exception cref="JsonException">Thrown when the token is not a string or cannot be parsed as a date.</exception>
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

    /// <summary>
    /// Writes a <see cref="DateTime"/> as an ISO 8601 ("O") formatted string.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The date and time value to write.</param>
    /// <param name="options">The serializer options in use.</param>
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
    /// <summary>
    /// Reads a decimal from a JSON number or from a numeric string using invariant culture.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to read.</param>
    /// <param name="typeToConvert">The type to convert to.</param>
    /// <param name="options">The serializer options in use.</param>
    /// <returns>The parsed decimal value.</returns>
    /// <exception cref="JsonException">Thrown when the token is neither a number nor a parsable string.</exception>
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

    /// <summary>
    /// Writes a <see cref="decimal"/> as a JSON number.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The decimal value to write.</param>
    /// <param name="options">The serializer options in use.</param>
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
    /// <summary>
    /// Registers <see cref="JsonFormatter"/> as a singleton implementation of <see cref="IJsonFormatter"/>.
    /// </summary>
    /// <param name="services">The service collection to add the formatter to.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddJsonFormatter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IJsonFormatter, JsonFormatter>();
        return services;
    }
}
