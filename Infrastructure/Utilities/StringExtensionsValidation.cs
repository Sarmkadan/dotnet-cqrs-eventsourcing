#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// Validation extension methods for <see cref="StringExtensions"/> operations.
/// Provides validation helpers to check if string operations would succeed
/// and throw descriptive exceptions when validation fails.
/// </summary>
public static class StringExtensionsValidation
{
    /// <summary>
    /// Validates that all string operations on <see cref="StringExtensions"/> would succeed.
    /// Returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The string value to validate. Can be null.</param>
    /// <returns>List of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate ToSlug behavior
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - ToSlug would return empty string");
        }

        // Validate ToCamelCase behavior
        if (string.IsNullOrEmpty(value) && value.Length > 0 && !char.IsLower(value[0]))
        {
            // This is actually fine - ToCamelCase handles it by returning the value as-is
        }

        // Validate ToPascalCase behavior
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - ToPascalCase would return empty string");
        }

        // Validate ToSnakeCase behavior
        if (string.IsNullOrEmpty(value))
        {
            // This is fine - ToSnakeCase handles it by returning the value as-is
        }

        // Validate IsValidEmail behavior
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - IsValidEmail would return false");
        }
        else if (!value.IsValidEmail())
        {
            problems.Add("String is not a valid email format - IsValidEmail would return false");
        }

        // Validate IsValidGuid behavior
        if (!string.IsNullOrEmpty(value) && !value.IsValidGuid())
        {
            problems.Add("String is not a valid GUID format - IsValidGuid would return false");
        }

        // Validate RemoveWhitespace behavior
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - RemoveWhitespace would return empty string");
        }

        // Validate AlphanumericOnly behavior
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - AlphanumericOnly would return empty string");
        }

        // Validate IsNumeric behavior
        if (!value.IsNumeric())
        {
            problems.Add("String contains non-digit characters - IsNumeric would return false");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if all string operations on <see cref="StringExtensions"/> would succeed.
    /// Returns true if the string is valid for all operations, false otherwise.
    /// </summary>
    /// <param name="value">The string value to check. Can be null.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(this string? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that all string operations on <see cref="StringExtensions"/> would succeed.
    /// Throws <see cref="ArgumentException"/> with a descriptive message listing all validation problems.
    /// </summary>
    /// <param name="value">The string value to validate. Can be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void EnsureValid(this string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("String validation failed:");

        foreach (var problem in problems)
        {
            sb.AppendLine($"- {problem}");
        }

        throw new ArgumentException(sb.ToString());
    }
}