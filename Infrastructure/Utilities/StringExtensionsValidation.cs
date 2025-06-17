#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// Validation extension methods for StringExtensions operations.
/// Provides validation helpers to check if string operations would succeed
/// and throw descriptive exceptions when validation fails.
/// </summary>
public static class StringExtensionsValidation
{
    /// <summary>
    /// Validates that all string operations on StringExtensions would succeed.
    /// Returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The string value to validate</param>
    /// <returns>List of validation problems (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(this string? value)
    {
        var problems = new List<string>();

        if (value is null)
        {
            problems.Add("String is null");
            return problems;
        }

        // Validate ToSlug: null/empty handled by method itself
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - ToSlug would return empty string");
        }

        // Validate ToCamelCase: null/empty handled by method itself
        if (string.IsNullOrEmpty(value) && value.Length > 0 && !char.IsLower(value[0]))
        {
            // This is actually fine - ToCamelCase handles it
        }

        // Validate ToPascalCase: null/empty handled by method itself
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - ToPascalCase would return empty string");
        }

        // Validate ToSnakeCase: null/empty handled by method itself
        if (string.IsNullOrEmpty(value))
        {
            // This is fine - ToSnakeCase handles it
        }

        // Validate Truncate: maxLength should be positive
        // Note: This is validated at call time, not here

        // Validate Repeat: count should be non-negative
        // Note: This is validated at call time, not here

        // Validate IsValidEmail: should be non-empty
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - IsValidEmail would return false");
        }

        // Validate IsValidGuid: should be valid GUID format
        if (!string.IsNullOrEmpty(value) && !value.IsValidGuid())
        {
            problems.Add("String is not a valid GUID format - IsValidGuid would return false");
        }

        // Validate RemoveWhitespace: null/empty handled by method itself
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - RemoveWhitespace would return empty string");
        }

        // Validate EnsureStartsWith: prefix should not be null
        // Note: This is validated at call time, not here

        // Validate EnsureEndsWith: suffix should not be null
        // Note: This is validated at call time, not here

        // Validate AlphanumericOnly: null/empty handled by method itself
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add("String is empty or whitespace - AlphanumericOnly would return empty string");
        }

        // Validate IsNumeric: should contain only digits
        if (!string.IsNullOrEmpty(value) && !value.IsNumeric())
        {
            problems.Add("String contains non-digit characters - IsNumeric would return false");
        }

        // Validate PadLeft: totalWidth should be non-negative
        // Note: This is validated at call time, not here

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if all string operations on StringExtensions would succeed.
    /// Returns true if the string is valid for all operations, false otherwise.
    /// </summary>
    /// <param name="value">The string value to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this string? value)
    {
        return !value.Validate().Any();
    }

    /// <summary>
    /// Ensures that all string operations on StringExtensions would succeed.
    /// Throws ArgumentException with a descriptive message listing all validation problems.
    /// </summary>
    /// <param name="value">The string value to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static void EnsureValid(this string? value)
    {
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