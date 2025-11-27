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
    public string Code { get; }
    public Dictionary<string, object> Metadata { get; }

    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message)
    {
        Code = code;
        Metadata = new Dictionary<string, object>();
    }

    public DomainException(string message, string code, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
        Metadata = new Dictionary<string, object>();
    }

    public DomainException WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        if (Metadata.Count == 0)
            return baseString;

        var metadataString = string.Join(", ", Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{baseString}\nMetadata: {metadataString}";
    }
}
