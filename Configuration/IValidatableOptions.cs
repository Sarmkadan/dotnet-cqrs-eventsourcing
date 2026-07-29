namespace DotNetCqrsEventSourcing.Configuration;

/// <summary>
/// Defines an interface for configuration options that can be validated.
/// </summary>
public interface IValidatableOptions
{
    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if any option is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any option is out of valid range.</exception>
    void Validate();
}
