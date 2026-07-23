#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Exceptions;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict occurs during event persistence.
/// This indicates that another process has modified the aggregate while this one was being processed.
/// </summary>
public class ConcurrencyException : DotnetCqrsEventsourcingException
{
    /// <summary>
    /// Gets the aggregate ID that experienced the concurrency conflict.
    /// </summary>
    public string AggregateId { get; }

    /// <summary>
    /// Gets the expected version that was required.
    /// </summary>
    public long ExpectedVersion { get; }

    /// <summary>
    /// Gets the actual version that exists in the event store.
    /// </summary>
    public long ActualVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version in the store.</param>
    public ConcurrencyException(string message, string aggregateId, long expectedVersion, long actualVersion)
        : base(message, "CONCURRENCY_CONFLICT")
    {
        AggregateId = aggregateId ?? throw new ArgumentNullException(nameof(aggregateId));
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    /// <summary>
    /// Creates a new ConcurrencyException with the specified parameters.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version in the store.</param>
    /// <returns>A new ConcurrencyException instance.</returns>
    public static ConcurrencyException ForAggregate(string aggregateId, long expectedVersion, long actualVersion)
    {
        var message = $"Concurrency conflict for aggregate '{aggregateId}'. Expected version {expectedVersion}, but actual version is {actualVersion}.";
        return new ConcurrencyException(message, aggregateId, expectedVersion, actualVersion);
    }

    public override string ToString()
    {
        return $"[{ErrorCode}] {GetType().Name} {{ AggregateId={AggregateId}, ExpectedVersion={ExpectedVersion}, ActualVersion={ActualVersion} }} - {Message}";
    }
}
