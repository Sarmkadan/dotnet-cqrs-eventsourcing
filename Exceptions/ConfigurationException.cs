using System;

namespace Exceptions;

/// <summary>
/// Represents errors that occur due to invalid or missing configuration.
/// </summary>
public class ConfigurationException : DotnetCqrsEventsourcingException
{
    public ConfigurationException()
    {
    }

    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
