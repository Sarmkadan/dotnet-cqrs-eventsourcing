#nullable enable

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Provides validation helpers for <see cref="ValidationException"/> instances.
/// </summary>
public static class ValidationExceptionValidation
{
    /// <summary>
    /// Validates that the <see cref="ValidationException"/> instance contains valid data.
    /// </summary>
    /// <param name="value">The exception to validate.</param>
    /// <returns>A list of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ValidationException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate ValidationErrors dictionary
        if (value.ValidationErrors is null)
        {
            problems.Add("ValidationErrors dictionary is null.");
        }
        else if (value.ValidationErrors.Count == 0)
        {
            problems.Add("ValidationErrors dictionary is empty.");
        }
        else
        {
            foreach (var kvp in value.ValidationErrors)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                {
                    problems.Add("ValidationErrors contains an entry with null or empty field name.");
                }

                if (string.IsNullOrEmpty(kvp.Value))
                {
                    problems.Add($"ValidationErrors['{kvp.Key}'] has null or empty error message.");
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="ValidationException"/> instance is valid.
    /// </summary>
    /// <param name="value">The exception to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ValidationException value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the <see cref="ValidationException"/> instance is valid, throwing an <see cref="ArgumentException"/>
    /// if validation fails with a list of problems.
    /// </summary>
    /// <param name="value">The exception to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing the list of problems.</exception>
    public static void EnsureValid(this ValidationException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ValidationException is invalid. Problems:\n- {
                    string.Join("\n- ", problems)
                }");
        }
    }
}