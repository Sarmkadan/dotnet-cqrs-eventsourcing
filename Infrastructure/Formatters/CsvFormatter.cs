#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using System.Text;
using System.Reflection;

namespace DotNetCqrsEventSourcing.Infrastructure.Formatters;

/// <summary>
/// CSV output formatter for exporting objects and collections to CSV format.
/// Reflects on object properties to determine columns automatically.
/// Handles string escaping, null values, and custom column ordering.
/// Useful for exporting data to Excel, data analytics, or audit reports.
/// </summary>
public interface ICsvFormatter
{
    /// <summary>
    /// Formats a collection of objects to CSV with headers.
    /// </summary>
    string Format<T>(IEnumerable<T> items, CsvFormatOptions? options = null);

    /// <summary>
    /// Formats objects to CSV without headers (raw data).
    /// </summary>
    string FormatWithoutHeaders<T>(IEnumerable<T> items, CsvFormatOptions? options = null);

    /// <summary>
    /// Gets available properties of a type as potential CSV columns.
    /// </summary>
    IEnumerable<string> GetColumns<T>();
}

/// <summary>
/// Default implementation of <see cref="ICsvFormatter"/> that reflects on object
/// properties to produce CSV output.
/// </summary>
public class CsvFormatter : ICsvFormatter
{
    /// <summary>
    /// Formats a collection of objects to CSV with headers.
    /// </summary>
    /// <typeparam name="T">The type of items to format.</typeparam>
    /// <param name="items">The collection of objects to format.</param>
    /// <param name="options">Optional formatting options; defaults are used when null.</param>
    /// <returns>The CSV representation of the items, or an empty string when no items are provided.</returns>
    public string Format<T>(IEnumerable<T> items, CsvFormatOptions? options = null)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0)
        {
            return string.Empty;
        }

        var opts = options ?? CsvFormatOptions.Default();
        var columns = GetOrderedColumns<T>();

        var sb = new StringBuilder();

        // Write headers
        if (opts.IncludeHeaders)
        {
            var headers = columns.Select(c => EscapeCsvValue(c.Header, opts.Delimiter));
            sb.AppendLine(string.Join(opts.Delimiter, headers));
        }

        // Write data rows
        foreach (var item in itemsList)
        {
            var values = columns.Select(c => GetPropertyValue(item, c.PropertyName));
            var csvRow = string.Join(
                opts.Delimiter,
                values.Select(v => EscapeCsvValue(FormatValue(v, opts), opts.Delimiter)));
            sb.AppendLine(csvRow);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats objects to CSV without headers (raw data).
    /// </summary>
    /// <typeparam name="T">The type of items to format.</typeparam>
    /// <param name="items">The collection of objects to format.</param>
    /// <param name="options">Optional formatting options; defaults are used when null.</param>
    /// <returns>The CSV representation of the items without a header row.</returns>
    public string FormatWithoutHeaders<T>(IEnumerable<T> items, CsvFormatOptions? options = null)
    {
        var source = options ?? CsvFormatOptions.Default();

        // Copy the options so the caller's instance is not mutated.
        var opts = new CsvFormatOptions
        {
            Delimiter = source.Delimiter,
            DateFormat = source.DateFormat,
            IncludeHeaders = false
        };

        return Format(items, opts);
    }

    /// <summary>
    /// Gets available properties of a type as potential CSV columns.
    /// </summary>
    /// <typeparam name="T">The type whose properties to inspect.</typeparam>
    /// <returns>The property names of <typeparamref name="T"/> that are eligible for CSV export.</returns>
    public IEnumerable<string> GetColumns<T>()
    {
        return GetOrderedColumns<T>().Select(c => c.PropertyName);
    }

    /// <summary>
    /// Resolves exportable columns for a type, honoring <see cref="CsvIgnoreAttribute"/>,
    /// <see cref="CsvColumnAttribute.Name"/> for headers, and <see cref="CsvColumnAttribute.Order"/>
    /// for column ordering (unannotated properties keep declaration order at the end).
    /// </summary>
    private static List<(string PropertyName, string Header)> GetOrderedColumns<T>()
    {
        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !p.GetCustomAttributes<CsvIgnoreAttribute>().Any())
            .Select((p, index) => (Property: p, Index: index, Column: p.GetCustomAttribute<CsvColumnAttribute>()))
            .OrderBy(x => x.Column?.Order ?? int.MaxValue)
            .ThenBy(x => x.Index)
            .Select(x => (x.Property.Name, x.Column?.Name ?? x.Property.Name))
            .ToList();
    }

    /// <summary>
    /// Converts a property value to its CSV string form using the invariant culture,
    /// applying <see cref="CsvFormatOptions.DateFormat"/> to date values.
    /// </summary>
    private static string FormatValue(object? value, CsvFormatOptions options) => value switch
    {
        null => string.Empty,
        DateTime dateTime => dateTime.ToString(options.DateFormat, CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(options.DateFormat, CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    /// <summary>
    /// Extracts property value from an object.
    /// Handles nested properties with dot notation (e.g., "User.Name").
    /// </summary>
    private static object? GetPropertyValue<T>(T item, string propertyName)
    {
        if (item is null) return null;

        var parts = propertyName.Split('.');
        object? current = item;

        foreach (var part in parts)
        {
            if (current is null) return null;

            var property = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
            if (property is null) return null;

            current = property.GetValue(current);
        }

        return current;
    }

    /// <summary>
    /// Escapes CSV field values: quotes, newlines, and the active delimiter require quoting.
    /// </summary>
    private static string EscapeCsvValue(string value, char delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuoting = value.Contains('"') || value.Contains(delimiter) || value.Contains('\n') || value.Contains('\r');

        if (needsQuoting)
        {
            // Escape quotes by doubling them
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

/// <summary>
/// Options that control how <see cref="CsvFormatter"/> produces CSV output.
/// </summary>
public sealed class CsvFormatOptions
{
    /// <summary>
    /// The character used to separate fields in the CSV output.
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Whether a header row is written before the data rows.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// The format string applied to date values.
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Creates a new instance with default formatting options.
    /// </summary>
    public static CsvFormatOptions Default() => new();

    /// <summary>
    /// Creates a new instance configured with a semicolon delimiter.
    /// </summary>
    public static CsvFormatOptions WithSemicolonDelimiter() => new() { Delimiter = ';' };

    /// <summary>
    /// Creates a new instance configured with a tab delimiter.
    /// </summary>
    public static CsvFormatOptions WithTabDelimiter() => new() { Delimiter = '\t' };
}

/// <summary>
/// Attribute to exclude properties from CSV export.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CsvIgnoreAttribute : Attribute
{
}

/// <summary>
/// Attribute to customize CSV column name for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CsvColumnAttribute : Attribute
{
    /// <summary>
    /// The column name to use in the CSV header for the annotated property.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The position of the column in the CSV output; lower values appear first.
    /// </summary>
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// Initializes a new instance with the specified column name.
    /// </summary>
    /// <param name="name">The column name to use in the CSV header.</param>
    public CsvColumnAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Extension methods for registering CSV formatter.
/// </summary>
public static class CsvFormatterExtensions
{
    /// <summary>
    /// Registers <see cref="ICsvFormatter"/> and its <see cref="CsvFormatter"/> implementation
    /// as a singleton in the service collection.
    /// </summary>
    /// <param name="services">The service collection to register the formatter with.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCsvFormatter(this IServiceCollection services)
    {
        services.AddSingleton<ICsvFormatter, CsvFormatter>();
        return services;
    }
}
