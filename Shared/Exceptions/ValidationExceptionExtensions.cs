using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCqrsEventSourcing.Shared.Exceptions
{
    /// <summary>
    /// Provides extension methods for working with <see cref="ValidationException"/> instances.
    /// </summary>
    public static class ValidationExceptionExtensions
    {
        /// <summary>
        /// Adds a validation error to the exception's ValidationErrors dictionary.
        /// </summary>
        /// <param name="exception">The validation exception to extend.</param>
        /// <param name="propertyName">The name of the property being validated.</param>
        /// <param name="errorMessage">The validation error message.</param>
        /// <returns>A new ValidationException with the additional error added.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> or <paramref name="errorMessage"/> is null or whitespace.</exception>
        public static ValidationException WithError(
            this ValidationException exception,
            string propertyName,
            string errorMessage)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
            ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

            var newErrors = new Dictionary<string, string>(exception.ValidationErrors);
            newErrors[propertyName] = errorMessage;

            var result = new ValidationException(exception.Message, exception.ErrorCode);
            foreach (var error in newErrors)
            {
                result.ValidationErrors[error.Key] = error.Value;
            }

            return result;
        }

        /// <summary>
        /// Combines multiple validation exceptions into a single aggregated exception.
        /// </summary>
        /// <param name="exceptions">Collection of validation exceptions to aggregate.</param>
        /// <returns>A new ValidationException containing all validation errors from all exceptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exceptions"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when no valid validation exceptions are provided.</exception>
        public static ValidationException AggregateAll(this IEnumerable<ValidationException> exceptions)
        {
            ArgumentNullException.ThrowIfNull(exceptions);

            var allErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var exception in exceptions.Where(e => e != null))
            {
                foreach (var error in exception.ValidationErrors)
                {
                    if (!allErrors.ContainsKey(error.Key))
                    {
                        allErrors[error.Key] = error.Value;
                    }
                }
            }

            if (allErrors.Count == 0)
            {
                throw new ArgumentException("No valid validation exceptions provided", nameof(exceptions));
            }

            var result = new ValidationException("Multiple validation errors occurred.");
            foreach (var error in allErrors)
            {
                result.ValidationErrors[error.Key] = error.Value;
            }

            return result;
        }

        /// <summary>
        /// Checks if the validation exception contains an error for the specified property.
        /// </summary>
        /// <param name="exception">The validation exception to check.</param>
        /// <param name="propertyName">The property name to check for.</param>
        /// <returns>True if the property has a validation error; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public static bool HasErrorFor(this ValidationException exception, string propertyName)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.ValidationErrors.ContainsKey(propertyName);
        }

        /// <summary>
        /// Gets the error message for the specified property, or null if no error exists.
        /// </summary>
        /// <param name="exception">The validation exception to query.</param>
        /// <param name="propertyName">The property name to get the error for.</param>
        /// <returns>The error message if found; otherwise null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        public static string GetErrorMessage(this ValidationException exception, string propertyName)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentNullException.ThrowIfNull(propertyName);

            return exception.ValidationErrors.TryGetValue(propertyName, out var errorMessage)
                ? errorMessage
                : null;
        }
    }
}