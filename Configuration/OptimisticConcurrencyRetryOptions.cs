#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Configuration;

/// <summary>
/// Configuration options for optimistic concurrency retry policy.
/// </summary>
public sealed class OptimisticConcurrencyRetryOptions
{
    /// <summary>
    /// Configuration section name used when binding from <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "OptimisticConcurrencyRetry";

    /// <summary>
    /// Gets or sets whether optimistic concurrency retry is enabled.
    /// When enabled, operations that fail due to concurrency conflicts will automatically retry.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts when a concurrency conflict occurs.
    /// Must be between 1 and 100.
    /// Defaults to 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay in milliseconds for the first retry attempt.
    /// Each subsequent retry will use exponential backoff: delay * 2^(attempt - 1).
    /// Must be between 10 and 10000.
    /// Defaults to 50 milliseconds.
    /// </summary>
    public int BaseDelayMilliseconds { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds for any retry attempt.
    /// Prevents excessively long delays for repeated conflicts.
    /// Must be between 100 and 30000.
    /// Defaults to 2000 milliseconds (2 seconds).
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 2000;

    /// <summary>
    /// Gets or sets a random jitter factor to add variability to retry delays.
    /// The actual delay will be between [delay * (1 - JitterFactor)] and [delay * (1 + JitterFactor)].
    /// Must be between 0.0 and 0.5.
    /// Defaults to 0.1 (10% jitter).
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any option is out of valid range.</exception>
    public void Validate()
    {
        if (MaxRetryAttempts < 1 || MaxRetryAttempts > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxRetryAttempts),
                $"MaxRetryAttempts must be between 1 and 100, but was {MaxRetryAttempts}.");
        }

        if (BaseDelayMilliseconds < 10 || BaseDelayMilliseconds > 10000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BaseDelayMilliseconds),
                $"BaseDelayMilliseconds must be between 10 and 10000, but was {BaseDelayMilliseconds}.");
        }

        if (MaxDelayMilliseconds < 100 || MaxDelayMilliseconds > 30000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxDelayMilliseconds),
                $"MaxDelayMilliseconds must be between 100 and 30000, but was {MaxDelayMilliseconds}.");
        }

        if (BaseDelayMilliseconds > MaxDelayMilliseconds)
        {
            throw new ArgumentException(
                $"BaseDelayMilliseconds ({BaseDelayMilliseconds}) cannot be greater than MaxDelayMilliseconds ({MaxDelayMilliseconds}).");
        }

        if (JitterFactor < 0.0 || JitterFactor > 0.5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(JitterFactor),
                $"JitterFactor must be between 0.0 and 0.5, but was {JitterFactor}.");
        }
    }
}
