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
    public ConfigurationException(string message, string errorCode = "CONFIGURATION_ERROR")
        : base(message, errorCode)
    {
    }

    public ConfigurationException(string message, string errorCode, Exception? innerException)
        : base(message, errorCode, innerException)
    {
    }

    public static ConfigurationException MissingRequiredConfiguration(string configurationKey)
        => new ConfigurationException(
            $"Required configuration '{configurationKey}' is missing or empty.");

    public static ConfigurationException InvalidConfigurationValue(string configurationKey, string value)
        => new ConfigurationException(
            $"Configuration '{configurationKey}' has invalid value: '{value}'.");

    public static ConfigurationException ValidationFailed(string validationMessage)
        => new ConfigurationException(validationMessage, "CONFIGURATION_VALIDATION_FAILED");
}