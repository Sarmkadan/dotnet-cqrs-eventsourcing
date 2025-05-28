using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCqrsEventSourcing.Shared.Exceptions
{
    public static class ValidationExceptionExtensions
    {
        /// <summary>
        /// Adds a validation error to the exception's ValidationErrors dictionary.
        /// </summary>
        /// <param name="exception">The validation exception to extend</param>
        /// <param name="propertyName">The name of the property being validated</param>
        /// <param name="errorMessage">The validation error message</param>
        /// <returns>A new ValidationException with the additional error added</returns>
        public static ValidationException WithError(
            this ValidationException exception,
            string propertyName,
            string errorMessage)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException("Property name cannot be null or whitespace", nameof(propertyName));
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or whitespace", nameof(errorMessage));
            }

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
        /// <param name="exceptions">Collection of validation exceptions to aggregate</param>
        /// <returns>A new ValidationException containing all validation errors from all exceptions</returns>
        public static ValidationException AggregateAll(this IEnumerable<ValidationException> exceptions)
        {
            if (exceptions == null)
            {
                throw new ArgumentNullException(nameof(exceptions));
            }

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
        /// <param name="exception">The validation exception to check</param>
        /// <param name="propertyName">The property name to check for</param>
        /// <returns>True if the property has a validation error; otherwise false</returns>
        public static bool HasErrorFor(this ValidationException exception, string propertyName)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return exception.ValidationErrors.ContainsKey(propertyName);
        }

        /// <summary>
        /// Gets the error message for the specified property, or null if no error exists.
        /// </summary>
        /// <param name="exception">The validation exception to query</param>
        /// <param name="propertyName">The property name to get the error for</param>
        /// <returns>The error message if found; otherwise null</returns>
        public static string GetErrorMessage(this ValidationException exception, string propertyName)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (exception.ValidationErrors.TryGetValue(propertyName ?? string.Empty, out var errorMessage))
            {
                return errorMessage;
            }

            return null;
        }
    }
}