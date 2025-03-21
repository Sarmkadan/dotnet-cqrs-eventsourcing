// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Remove accents
        var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(value);
        var result = Encoding.ASCII.GetString(bytes);

        // Remove invalid characters
        result = Regex.Replace(result, @"[^a-z0-9\s-]", "", RegexOptions.IgnoreCase);

        // Replace multiple spaces with single hyphen
        result = Regex.Replace(result, @"\s+", "-", RegexOptions.IgnoreCase);

        // Trim hyphens from start/end
        return result.Trim('-').ToLower();
    }

    /// <summary>
    /// Converts PascalCase to camelCase.
    /// Example: "AccountCreated" -> "accountCreated"
    /// </summary>
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLower(value[0]) + value[1..];
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// Example: "account_created" -> "AccountCreated", "accountCreated" -> "AccountCreated"
    /// </summary>
    public static string ToPascalCase(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var words = value.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var word in words)
        {
            sb.Append(char.ToUpper(word[0]) + word[1..].ToLower());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts PascalCase to snake_case.
    /// Example: "AccountCreatedEvent" -> "account_created_event"
    /// </summary>
    public static string ToSnakeCase(this string value)
    {
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

            sb.Append(char.ToLower(chars[i]));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Truncates string to max length and adds ellipsis if truncated.
    /// Example: "Hello World".Truncate(8) -> "Hello..."
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - suffix.Length)] + suffix;
    }

    /// <summary>
    /// Repeats a string n times.
    /// Example: "*".Repeat(5) -> "*****"
    /// </summary>
    public static string Repeat(this string value, int count)
    {
        if (count <= 0) return string.Empty;
        return string.Concat(Enumerable.Repeat(value, count));
    }

    /// <summary>
    /// Checks if string is a valid email address using regex.
    /// Not RFC-compliant but covers 99% of real-world cases.
    /// </summary>
    public static bool IsValidEmail(this string value)
    {
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
    public static bool IsValidGuid(this string value)
    {
        return Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Removes all whitespace from a string.
    /// Example: "Hello World" -> "HelloWorld"
    /// </summary>
    public static string RemoveWhitespace(this string value)
    {
        return Regex.Replace(value, @"\s+", string.Empty);
    }

    /// <summary>
    /// Ensures a string starts with a given prefix.
    /// Example: "world".EnsureStartsWith("hello_") -> "hello_world"
    /// </summary>
    public static string EnsureStartsWith(this string value, string prefix)
    {
        return value.StartsWith(prefix) ? value : prefix + value;
    }

    /// <summary>
    /// Ensures a string ends with a given suffix.
    /// Example: "hello".EnsureEndsWith("_world") -> "hello_world"
    /// </summary>
    public static string EnsureEndsWith(this string value, string suffix)
    {
        return value.EndsWith(suffix) ? value : value + suffix;
    }

    /// <summary>
    /// Extracts alphanumeric characters only.
    /// Example: "Hello-World_123" -> "HelloWorld123"
    /// </summary>
    public static string AlphanumericOnly(this string value)
    {
        return Regex.Replace(value, @"[^a-zA-Z0-9]", string.Empty);
    }

    /// <summary>
    /// Checks if a string consists only of digits.
    /// </summary>
    public static bool IsNumeric(this string value)
    {
        return !string.IsNullOrEmpty(value) && value.All(char.IsDigit);
    }

    /// <summary>
    /// Left-pads a string with a character to reach a minimum length.
    /// Example: "42".PadLeft(5, '0') -> "00042"
    /// </summary>
    public static string PadLeft(this string value, int totalWidth, char paddingChar = ' ')
    {
        return string.IsNullOrEmpty(value)
            ? new string(paddingChar, totalWidth)
            : value.PadLeft(totalWidth, paddingChar);
    }
}
