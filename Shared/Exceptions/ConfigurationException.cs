#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Exception thrown when there is an error in application configuration or validation.
/// </summary>
public class ConfigurationException : DotnetCqrsEventsourcingException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with the specified message and error code.
    /// </summary>
    /// <param name="message">The message that describes the configuration error.</param>
    /// <param name="errorCode">The machine-readable error code. Defaults to <c>CONFIGURATION_ERROR</c>.</param>
    public ConfigurationException(string message, string errorCode = "CONFIGURATION_ERROR")
        : base(message, errorCode)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNullOrEmpty(errorCode);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with the specified message, error code and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the configuration error.</param>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="innerException">The exception that caused the current exception, or <see langword="null"/>.</param>
    public ConfigurationException(string message, string errorCode, Exception? innerException)
        : base(message, errorCode, innerException)
    {
        ArgumentNullException.ThrowIfNull(message);
    }

    /// <summary>
    /// Creates an exception indicating that a required configuration value is missing or empty.
    /// </summary>
    /// <param name="configurationKey">The name of the missing configuration key.</param>
    /// <returns>A new <see cref="ConfigurationException"/> describing the missing configuration.</returns>
    public static ConfigurationException MissingRequiredConfiguration(string configurationKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(configurationKey);
        return new ConfigurationException(
            $"Required configuration '{configurationKey}' is missing or empty.");
    }

    /// <summary>
    /// Creates an exception indicating that a configuration key has an invalid value.
    /// </summary>
    /// <param name="configurationKey">The name of the configuration key.</param>
    /// <param name="value">The invalid value that was supplied.</param>
    /// <returns>A new <see cref="ConfigurationException"/> describing the invalid value.</returns>
    public static ConfigurationException InvalidConfigurationValue(string configurationKey, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(configurationKey);
        ArgumentException.ThrowIfNullOrEmpty(value);
        return new ConfigurationException(
            $"Configuration '{configurationKey}' has invalid value: '{value}'.");
    }

    /// <summary>
    /// Creates an exception indicating that configuration validation failed.
    /// </summary>
    /// <param name="validationMessage">The message describing the validation failure.</param>
    /// <returns>A new <see cref="ConfigurationException"/> with the <c>CONFIGURATION_VALIDATION_FAILED</c> error code.</returns>
    public static ConfigurationException ValidationFailed(string validationMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(validationMessage);
        return new ConfigurationException(validationMessage, "CONFIGURATION_VALIDATION_FAILED");
    }
}