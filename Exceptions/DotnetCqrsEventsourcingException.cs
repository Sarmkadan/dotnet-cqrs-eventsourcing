using System;

namespace Exceptions;

/// <summary>
/// Base exception type for the DotnetCqrsEventsourcing application.
/// </summary>
public class DotnetCqrsEventsourcingException : Exception
{
    public DotnetCqrsEventsourcingException()
    {
    }

    public DotnetCqrsEventsourcingException(string message) : base(message)
    {
    }

    public DotnetCqrsEventsourcingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
