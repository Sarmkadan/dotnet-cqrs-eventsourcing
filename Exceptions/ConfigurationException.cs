namespace Exceptions;

/// <summary>
/// Represents errors that occur due to invalid or missing configuration.
/// </summary>
public class ConfigurationException : DotnetCqrsEventsourcingException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with the specified error message.
    /// </summary>
    /// <param name="message">The message that describes the configuration error.</param>
    public ConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with the specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the configuration error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
