#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Base exception for domain-level errors in the CQRS framework.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Gets the machine-readable code identifying the domain error.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the additional key/value details attached to the error.
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the domain error.</param>
    /// <param name="code">The machine-readable error code. Defaults to <c>DOMAIN_ERROR</c>.</param>
    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message)
    {
        Code = code;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the domain error.</param>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public DomainException(string message, string code, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Adds a metadata entry to the exception. An existing entry with the same key is replaced.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This exception instance, to allow fluent chaining.</returns>
    public DomainException WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Returns a string representation of the exception including its error code and metadata.
    /// </summary>
    /// <returns>A formatted string describing the exception.</returns>
    public override string ToString()
    {
        var baseString = base.ToString();
        var codePart = $"Code: {Code}";
        if (Metadata.Count == 0)
        {
            return $"{baseString}\n{codePart}";
        }

        var metadataString = string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{baseString}\n{codePart}\nMetadata: {metadataString}";
    }
}
