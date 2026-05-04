// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// Guard clause helper methods for input validation at method boundaries.
/// Reduces boilerplate null/empty checks and provides consistent error messages.
/// Guards should be placed at the start of public methods to fail fast on invalid input.
/// Prevents null reference exceptions and invalid state propagation deep into the application.
/// </summary>
public static class GuardClauses
{
    /// <summary>
    /// Throws if the argument is null. Use for required reference types.
    /// Provides better error message than System.ArgumentNullException with parameter name.
    /// </summary>
    public static T NotNull<T>(T? argument, string parameterName) where T : class
    {
        if (argument is null)
        {
            throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null");
        }

        return argument;
    }

    /// <summary>
    /// Throws if the string is null or empty. Use for required text fields.
    /// Prevents silent failures from whitespace-only strings.
    /// </summary>
    public static string NotNullOrEmpty(string? argument, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
        }

        return argument;
    }

    /// <summary>
    /// Throws if the collection is null or empty. Use for required collections.
    /// </summary>
    public static IEnumerable<T> NotNullOrEmpty<T>(IEnumerable<T>? collection, string parameterName)
    {
        if (collection is null || !collection.Any())
        {
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
        }

        return collection;
    }

    /// <summary>
    /// Throws if the value is outside the valid range.
    /// Useful for amounts, percentages, and other bounded values.
    /// </summary>
    public static T InRange<T>(T value, T min, T max, string parameterName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"{parameterName} must be between {min} and {max}, but got {value}"
            );
        }

        return value;
    }

    /// <summary>
    /// Throws if the value is negative. Use for amounts, counts, and indices.
    /// </summary>
    public static T NotNegative<T>(T value, string parameterName) where T : IComparable<T>
    {
        if (value.CompareTo(default(T)!) < 0)
        {
            throw new ArgumentException($"{parameterName} cannot be negative", parameterName);
        }

        return value;
    }

    /// <summary>
    /// Throws if the value is zero. Use when zero is invalid.
    /// </summary>
    public static T NotZero<T>(T value, string parameterName) where T : struct, IComparable<T>
    {
        if (value.CompareTo(default(T)) == 0)
        {
            throw new ArgumentException($"{parameterName} cannot be zero", parameterName);
        }

        return value;
    }

    /// <summary>
    /// Throws if the condition is false. Use for domain-specific invariants.
    /// Provides explicit validation for business rules that can't be expressed with type system.
    /// </summary>
    public static void Condition(bool condition, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message);
        }
    }

    /// <summary>
    /// Throws if the GUID is empty. Use for required aggregate IDs.
    /// </summary>
    public static Guid NotEmpty(Guid guid, string parameterName)
    {
        if (guid == Guid.Empty)
        {
            throw new ArgumentException($"{parameterName} cannot be empty GUID", parameterName);
        }

        return guid;
    }

    /// <summary>
    /// Throws if the string doesn't match the pattern. Use for format validation.
    /// </summary>
    public static string Matches(string value, string pattern, string parameterName)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
        {
            throw new ArgumentException($"{parameterName} does not match required pattern: {pattern}", parameterName);
        }

        return value;
    }
}
