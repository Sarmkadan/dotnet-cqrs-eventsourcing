#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// String manipulation extension methods for common operations.
/// Includes slugification, case conversion, padding, and formatting helpers.
/// Used throughout the framework for URL generation, logging, and data transformation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to URL-friendly slug (lowercase, hyphens, no special chars).
    /// Example: "Hello World" -> "hello-world", "User.Created" -> "user-created"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static string ToSlug(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Normalize to form D (decomposed) to properly handle accents
        var normalized = value.Normalize(NormalizationForm.FormD);

        // Remove diacritics and invalid characters
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark &&
                !char.IsPunctuation(c) &&
                !char.IsSymbol(c) &&
                c != '_')
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();

        // Replace whitespace and separators with hyphens
        result = Regex.Replace(result, @"[\s-]+", "-");

        // Remove any remaining invalid characters
        result = Regex.Replace(result, @"[^a-z0-9-]", string.Empty, RegexOptions.IgnoreCase);

        // Trim hyphens from start/end and collapse multiple hyphens
        result = result.Trim('-');
        result = Regex.Replace(result, @"-+", "-");

        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Converts PascalCase to camelCase.
    /// Example: "AccountCreated" -> "accountCreated"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static string ToCamelCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// Example: "account_created" -> "AccountCreated", "accountCreated" -> "AccountCreated"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static string ToPascalCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var words = value.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant());
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts PascalCase to snake_case.
    /// Example: "AccountCreatedEvent" -> "account_created_event"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static string ToSnakeCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var sb = new StringBuilder();
        var chars = value.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsUpper(chars[i]) && i > 0)
            {
                sb.Append('_');
            }

            sb.Append(char.ToLowerInvariant(chars[i]));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Truncates string to max length and adds ellipsis if truncated.
    /// Example: "Hello World".Truncate(8) -> "Hello..."
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is negative</exception>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Repeats a string n times.
    /// Example: "*".Repeat(5) -> "*****"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative</exception>
    public static string Repeat(this string value, int count)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return string.Concat(Enumerable.Repeat(value, count));
    }

    /// <summary>
    /// Checks if string is a valid email address using regex.
    /// Not RFC-compliant but covers 99% of real-world cases.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static bool IsValidEmail(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(value, pattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if string is valid UUID/GUID format.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static bool IsValidGuid(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Removes all whitespace from a string.
    /// Example: "Hello World" -> "HelloWorld"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static string RemoveWhitespace(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Regex.Replace(value, @"\s+", string.Empty);
    }

    /// <summary>
    /// Ensures a string starts with a given prefix.
    /// Example: "world".EnsureStartsWith("hello_") -> "hello_world"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <see langword="null"/></exception>
    public static string EnsureStartsWith(this string value, string prefix)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(prefix);

        return value.StartsWith(prefix) ? value : prefix + value;
    }

    /// <summary>
    /// Ensures a string ends with a given suffix.
    /// Example: "hello".EnsureEndsWith("_world") -> "hello_world"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentNullException"><paramref name="suffix"/> is <see langword="null"/></exception>
    public static string EnsureEndsWith(this string value, string suffix)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(suffix);

        return value.EndsWith(suffix) ? value : value + suffix;
    }

    /// <summary>
    /// Extracts alphanumeric characters only.
    /// Example: "Hello-World_123" -> "HelloWorld123"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static string AlphanumericOnly(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Regex.Replace(value, @"[^a-zA-Z0-9]", string.Empty);
    }

    /// <summary>
    /// Checks if a string consists only of digits.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static bool IsNumeric(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.All(char.IsDigit);
    }

    /// <summary>
    /// Left-pads a string with a character to reach a minimum length.
    /// Example: "42".PadLeft(5, '0') -> "00042"
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalWidth"/> is negative</exception>
    public static string PadLeft(this string value, int totalWidth, char paddingChar = ' ')
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(totalWidth);

        return value.PadLeft(totalWidth, paddingChar);
    }
}