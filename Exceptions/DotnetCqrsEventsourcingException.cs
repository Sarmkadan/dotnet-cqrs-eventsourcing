namespace Exceptions;

/// <summary>
/// Base exception type for the DotnetCqrsEventsourcing application.
/// </summary>
public class DotnetCqrsEventsourcingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetCqrsEventsourcingException"/> class.
    /// </summary>
    public DotnetCqrsEventsourcingException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetCqrsEventsourcingException"/> class with the specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DotnetCqrsEventsourcingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetCqrsEventsourcingException"/> class with the specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public DotnetCqrsEventsourcingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
