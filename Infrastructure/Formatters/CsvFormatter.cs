// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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

public class CsvFormatter : ICsvFormatter
{
    public string Format<T>(IEnumerable<T> items, CsvFormatOptions? options = null)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0)
        {
            return string.Empty;
        }

        var opts = options ?? CsvFormatOptions.Default();
        var properties = GetColumns<T>().ToList();

        var sb = new StringBuilder();

        // Write headers
        if (opts.IncludeHeaders)
        {
            var headers = properties.Select(p => EscapeCsvValue(p));
            sb.AppendLine(string.Join(opts.Delimiter, headers));
        }

        // Write data rows
        foreach (var item in itemsList)
        {
            var values = properties.Select(prop => GetPropertyValue(item, prop));
            var csvRow = string.Join(opts.Delimiter, values.Select(v => EscapeCsvValue(v?.ToString() ?? "")));
            sb.AppendLine(csvRow);
        }

        return sb.ToString();
    }

    public string FormatWithoutHeaders<T>(IEnumerable<T> items, CsvFormatOptions? options = null)
    {
        var opts = options ?? CsvFormatOptions.Default();
        opts.IncludeHeaders = false;
        return Format(items, opts);
    }

    public IEnumerable<string> GetColumns<T>()
    {
        var type = typeof(T);
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !p.GetCustomAttributes<CsvIgnoreAttribute>().Any())
            .Select(p => p.Name);
    }

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
    /// Escapes CSV field values: quotes, newlines, commas require quoting.
    /// </summary>
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuoting = value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r');

        if (needsQuoting)
        {
            // Escape quotes by doubling them
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public class CsvFormatOptions
{
    public char Delimiter { get; set; } = ',';
    public bool IncludeHeaders { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    public static CsvFormatOptions Default() => new();
    public static CsvFormatOptions WithSemicolonDelimiter() => new() { Delimiter = ';' };
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
    public string Name { get; set; }
    public int Order { get; set; } = int.MaxValue;

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
    public static IServiceCollection AddCsvFormatter(this IServiceCollection services)
    {
        services.AddSingleton<ICsvFormatter, CsvFormatter>();
        return services;
    }
}
