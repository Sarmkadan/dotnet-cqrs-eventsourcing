#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Extension methods for <see cref="ConfigurationException"/> that provide additional utility functions
/// for working with configuration errors in a fluent and practical way.
/// </summary>
public static class ConfigurationExceptionExtensions
{
    /// <summary>
    /// Creates a new ConfigurationException with additional context appended to the message.
    /// </summary>
    /// <param name="exception">The original configuration exception to extend.</param>
    /// <param name="additionalContext">Additional context to append to the error message. Must not be null or whitespace.</param>
    /// <returns>A new ConfigurationException with extended error message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static ConfigurationException WithContext(this ConfigurationException exception, string additionalContext)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.IsNullOrWhiteSpace(additionalContext)
            ? exception
            : new ConfigurationException(
                $"{exception.Message} {additionalContext}",
                exception.ErrorCode,
                exception.InnerException);
    }

    /// <summary>
    /// Creates a ConfigurationException for a missing configuration with a suggested default value.
    /// </summary>
    /// <param name="configurationKey">The configuration key that is missing.</param>
    /// <param name="suggestedDefault">Suggested default value to use instead.</param>
    /// <returns>A new ConfigurationException indicating the missing configuration with suggested alternative.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurationKey"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configurationKey"/> is empty or whitespace.</exception>
    public static ConfigurationException MissingWithSuggestion(string configurationKey, string suggestedDefault)
    {
        ArgumentException.ThrowIfNullOrEmpty(configurationKey, nameof(configurationKey));

        return new ConfigurationException(
            $"Required configuration '{configurationKey}' is missing. Consider using the suggested value: '{suggestedDefault}'.");
    }

    /// <summary>
    /// Creates a ConfigurationException for an invalid configuration value with additional validation details.
    /// </summary>
    /// <param name="configurationKey">The configuration key with invalid value.</param>
    /// <param name="invalidValue">The actual invalid value.</param>
    /// <param name="validationDetails">Detailed validation failure information.</param>
    /// <returns>A new ConfigurationException with comprehensive validation failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurationKey"/> or <paramref name="validationDetails"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configurationKey"/> or <paramref name="validationDetails"/> is empty or whitespace.</exception>
    public static ConfigurationException InvalidWithDetails(
        string configurationKey,
        string invalidValue,
        string validationDetails)
    {
        ArgumentException.ThrowIfNullOrEmpty(configurationKey, nameof(configurationKey));
        ArgumentException.ThrowIfNullOrEmpty(validationDetails, nameof(validationDetails));

        return new ConfigurationException(
            $"Configuration '{configurationKey}' has invalid value: '{invalidValue}'. Validation failed: {validationDetails}",
            "CONFIGURATION_VALIDATION_FAILED");
    }

    /// <summary>
    /// Creates a ConfigurationException from a collection of validation errors.
    /// </summary>
    /// <param name="validationErrors">Collection of validation error messages. Must not be null or empty.</param>
    /// <returns>A new ConfigurationException aggregating all validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validationErrors"/> is null.</exception>
    public static ConfigurationException FromValidationErrors(IEnumerable<string> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        if (!validationErrors.Any())
        {
            throw new ArgumentException("Validation errors collection cannot be empty", nameof(validationErrors));
        }

        var errors = string.Join("\n", validationErrors);
        return new ConfigurationException(
            $"Configuration validation failed:\n{errors}",
            "CONFIGURATION_VALIDATION_FAILED");
    }

    /// <summary>
    /// Creates a ConfigurationException for a collection of missing required configurations.
    /// </summary>
    /// <param name="missingKeys">Collection of missing configuration keys. Must not be null or empty.</param>
    /// <returns>A new ConfigurationException listing all missing configurations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="missingKeys"/> is null.</exception>
    public static ConfigurationException MissingMultiple(IEnumerable<string> missingKeys)
    {
        ArgumentNullException.ThrowIfNull(missingKeys);

        if (!missingKeys.Any())
        {
            throw new ArgumentException("Missing keys collection cannot be empty", nameof(missingKeys));
        }

        var keys = string.Join(", ", missingKeys);
        return new ConfigurationException(
            $"Required configurations are missing: {keys}");
    }
}