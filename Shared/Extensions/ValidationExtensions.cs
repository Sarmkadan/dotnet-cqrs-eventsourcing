#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Extensions;

using System.Net.Mail;
using Exceptions;

/// <summary>
/// Extension methods for validation and guard clauses.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Guard against null values with custom error message.
    /// </summary>
    /// <typeparam name="T">The type of the value being validated.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static T NotNull<T>(this T? value, string parameterName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        return value;
    }

    /// <summary>
    /// Guard against null or empty strings.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
    public static string NotNullOrEmpty(this string? value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, parameterName);
        return value;
    }

    /// <summary>
    /// Guard against null or empty strings with custom error message.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <param name="customMessage">Custom error message to use when validation fails.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
    public static string NotNullOrEmpty(this string? value, string parameterName, string customMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(customMessage, parameterName);
        }
        return value;
    }

    /// <summary>
    /// Guard against negative numbers.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is negative.</exception>
    public static decimal NotNegative(this decimal value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentException($"{parameterName} cannot be negative", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Guard against values outside a range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">Minimum allowed value (inclusive).</param>
    /// <param name="max">Maximum allowed value (inclusive).</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is outside the specified range.</exception>
    public static decimal InRange(this decimal value, decimal min, decimal max, string parameterName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentException($"{parameterName} must be between {min} and {max}", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Guard against invalid GUID.
    /// </summary>
    /// <param name="value">The string to validate as GUID.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a valid GUID.</exception>
    public static string ValidGuid(this string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (!Guid.TryParse(value, out _))
        {
            throw new ArgumentException($"{parameterName} is not a valid GUID", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Guard against invalid email format.
    /// </summary>
    /// <param name="value">The email address to validate.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated email address.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a valid email address.</exception>
    public static string ValidEmail(this string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        try
        {
            _ = new MailAddress(value);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            throw new ArgumentException($"{parameterName} is not a valid email address", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Guard against collections with no items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="collection"/> is empty.</exception>
    public static IEnumerable<T> NotEmpty<T>(this IEnumerable<T> collection, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(collection, parameterName);
        if (!collection.Any())
        {
            throw new ArgumentException($"{parameterName} cannot be empty", parameterName);
        }
        return collection;
    }

    /// <summary>
    /// Guard against string exceeding max length.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="maxLength">Maximum allowed length.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> exceeds maximum length.</exception>
    public static string MaxLength(this string value, int maxLength, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} exceeds maximum length of {maxLength} characters", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Guard against string shorter than min length.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="minLength">Minimum required length.</param>
    /// <param name="parameterName">Name of the parameter being validated.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is shorter than minimum length.</exception>
    public static string MinLength(this string value, int minLength, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (value.Length < minLength)
        {
            throw new ArgumentException($"{parameterName} must be at least {minLength} characters", parameterName);
        }
        return value;
    }

    /// <summary>
    /// Ensure condition is true, throw domain exception if false.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="errorMessage">Error message to include in the exception.</param>
    /// <param name="errorCode">Optional error code for the domain exception.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="condition"/> is false.</exception>
    public static void Ensure(this bool condition, string errorMessage, string errorCode = "VALIDATION_FAILED")
    {
        if (!condition)
        {
            throw new DomainException(errorMessage, errorCode);
        }
    }

    /// <summary>
    /// Get validation result for a field.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>A tuple indicating validity and optional error message.</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateRequired(this string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, $"{fieldName} is required");
        }
        return (true, null);
    }

    /// <summary>
    /// Get validation result for numeric range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">Minimum allowed value (inclusive).</param>
    /// <param name="max">Maximum allowed value (inclusive).</param>
    /// <param name="fieldName">Name of the field being validated.</param>
    /// <returns>A tuple indicating validity and optional error message.</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateRange(this decimal value, decimal min, decimal max, string fieldName)
    {
        if (value < min || value > max)
        {
            return (false, $"{fieldName} must be between {min} and {max}");
        }
        return (true, null);
    }
}