// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Extensions;

using Exceptions;

/// <summary>
/// Extension methods for validation and guard clauses.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Guard against null values with custom error message.
    /// </summary>
    public static T NotNull<T>(this T? value, string parameterName) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);
        return value;
    }

    /// <summary>
    /// Guard against null or empty strings.
    /// </summary>
    public static string NotNullOrEmpty(this string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
        return value;
    }

    /// <summary>
    /// Guard against negative numbers.
    /// </summary>
    public static decimal NotNegative(this decimal value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentException($"{parameterName} cannot be negative", parameterName);
        return value;
    }

    /// <summary>
    /// Guard against values outside a range.
    /// </summary>
    public static decimal InRange(this decimal value, decimal min, decimal max, string parameterName)
    {
        if (value < min || value > max)
            throw new ArgumentException($"{parameterName} must be between {min} and {max}", parameterName);
        return value;
    }

    /// <summary>
    /// Guard against invalid GUID.
    /// </summary>
    public static string ValidGuid(this string value, string parameterName)
    {
        if (!Guid.TryParse(value, out _))
            throw new ArgumentException($"{parameterName} is not a valid GUID", parameterName);
        return value;
    }

    /// <summary>
    /// Guard against invalid email format.
    /// </summary>
    public static string ValidEmail(this string value, string parameterName)
    {
        try
        {
            new System.Net.Mail.MailAddress(value);
        }
        catch
        {
            throw new ArgumentException($"{parameterName} is not a valid email address", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Guard against collections with no items.
    /// </summary>
    public static IEnumerable<T> NotEmpty<T>(this IEnumerable<T> collection, string parameterName)
    {
        if (!collection.Any())
            throw new ArgumentException($"{parameterName} cannot be empty", parameterName);
        return collection;
    }

    /// <summary>
    /// Guard against string exceeding max length.
    /// </summary>
    public static string MaxLength(this string value, int maxLength, string parameterName)
    {
        if (value.Length > maxLength)
            throw new ArgumentException(
                $"{parameterName} exceeds maximum length of {maxLength} characters",
                parameterName
            );
        return value;
    }

    /// <summary>
    /// Guard against string shorter than min length.
    /// </summary>
    public static string MinLength(this string value, int minLength, string parameterName)
    {
        if (value.Length < minLength)
            throw new ArgumentException(
                $"{parameterName} must be at least {minLength} characters",
                parameterName
            );
        return value;
    }

    /// <summary>
    /// Ensure condition is true, throw domain exception if false.
    /// </summary>
    public static void Ensure(this bool condition, string errorMessage, string errorCode = "VALIDATION_FAILED")
    {
        if (!condition)
            throw new DomainException(errorMessage, errorCode);
    }

    /// <summary>
    /// Get validation result for a field.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateRequired(this string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (false, $"{fieldName} is required");
        return (true, null);
    }

    /// <summary>
    /// Get validation result for numeric range.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateRange(this decimal value, decimal min, decimal max, string fieldName)
    {
        if (value < min || value > max)
            return (false, $"{fieldName} must be between {min} and {max}");
        return (true, null);
    }
}
