#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using Domain.Events;
using Shared.Results;

/// <summary>
/// In-memory implementation of event repository for testing and development.
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private readonly List<EventEnvelope> _events = new();
    private readonly object _lockObject = new();

    /// <summary>
    /// Saves a single event envelope to the in-memory store.
    /// </summary>
    /// <param name="eventEnvelope">The event envelope to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result, or a failure if the save violates version ordering.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventEnvelope"/> is <see langword="null"/>.</exception>
    public Task<Result> SaveEventAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventEnvelope);
        return SaveEventsAsync(new List<EventEnvelope> { eventEnvelope }, cancellationToken);
    }

    /// <summary>
    /// Saves a batch of event envelopes, checking optimistic concurrency per aggregate.
    /// </summary>
    /// <param name="envelopes">The event envelopes to save, in order.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result, or a <c>CONCURRENCY_CONFLICT</c> failure if an envelope's version is not greater than the stored version.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelopes"/> is <see langword="null"/>.</exception>
    public Task<Result> SaveEventsAsync(List<EventEnvelope> envelopes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelopes);
        try
        {
            lock (_lockObject)
            {
                foreach (var envelope in envelopes)
                {
                    // Check for optimistic concurrency
                    var aggregateEvents = _events.Where(e => e.AggregateId == envelope.AggregateId).ToList();
                    var maxVersion = aggregateEvents.Count > 0 ? aggregateEvents.Max(e => e.AggregateVersion) : -1;

                    if (envelope.AggregateVersion <= maxVersion)
                    {
                        return Task.FromResult(Result.Failure(
                            "CONCURRENCY_CONFLICT",
                            $"Concurrency conflict for aggregate {envelope.AggregateId}. Expected version > {maxVersion}, got {envelope.AggregateVersion}"
                        ));
                    }

                    envelope.ComputeChecksum();
                    _events.Add(envelope);
                }
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure("EVENT_SAVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves all stored events for the specified aggregate, ordered by version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the matching event envelopes.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is null or empty.</exception>
    public Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        try
        {
            lock (_lockObject)
            {
                var events = _events.Where(e => e.AggregateId == aggregateId).OrderBy(e => e.AggregateVersion).ToList();
                return Task.FromResult(Result<List<EventEnvelope>>.Success(events));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<List<EventEnvelope>>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves the events for the specified aggregate whose version is at least <paramref name="fromVersion"/>, ordered by version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="fromVersion">The minimum aggregate version to include (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the matching event envelopes.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is null or empty.</exception>
    public Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAndVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        try
        {
            lock (_lockObject)
            {
                var events = _events
                    .Where(e => e.AggregateId == aggregateId && e.AggregateVersion >= fromVersion)
                    .OrderBy(e => e.AggregateVersion)
                    .ToList();

                return Task.FromResult(Result<List<EventEnvelope>>.Success(events));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<List<EventEnvelope>>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves the events for the specified aggregate whose version lies within the given inclusive range, ordered by version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="fromVersion">The lower bound of the version range (inclusive).</param>
    /// <param name="toVersion">The upper bound of the version range (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the matching event envelopes, or an <c>INVALID_VERSION_RANGE</c> failure when <paramref name="toVersion"/> is less than <paramref name="fromVersion"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is null or empty.</exception>
    public Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdInVersionRangeAsync(string aggregateId, long fromVersion, long toVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);
        try
        {
            lock (_lockObject)
            {
                if (toVersion < fromVersion)
                {
                    return Task.FromResult(Result<List<EventEnvelope>>.Failure(
                        "INVALID_VERSION_RANGE",
                        $"Version range is invalid. From version {fromVersion} must be less than or equal to to version {toVersion}"
                    ));
                }

                var events = _events
                    .Where(e => e.AggregateId == aggregateId && e.AggregateVersion >= fromVersion && e.AggregateVersion <= toVersion)
                    .OrderBy(e => e.AggregateVersion)
                    .ToList();

                return Task.FromResult(Result<List<EventEnvelope>>.Success(events));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<List<EventEnvelope>>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves a single stored event by its identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the event envelope, or a <c>NOT_FOUND</c> failure if it does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventId"/> is null or empty.</exception>
    public Task<Result<EventEnvelope>> GetEventByIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventId);
        try
        {
            lock (_lockObject)
            {
                var @event = _events.FirstOrDefault(e => e.Id == eventId);
                if (@event is null)
                {
                    return Task.FromResult(Result<EventEnvelope>.Failure("NOT_FOUND", $"Event {eventId} not found"));
                }

                return Task.FromResult(Result<EventEnvelope>.Success(@event));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<EventEnvelope>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves all stored events of the specified event type, ordered by creation time and then version.
    /// </summary>
    /// <param name="eventType">The event type name to match.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the matching event envelopes.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null or empty.</exception>
    public Task<Result<List<EventEnvelope>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);
        try
        {
            lock (_lockObject)
            {
                var events = _events
                    .Where(e => e.EventType == eventType)
                    .OrderBy(e => e.CreatedAt)
                    .ThenBy(e => e.AggregateVersion)
                    .ToList();
                return Task.FromResult(Result<List<EventEnvelope>>.Success(events));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<List<EventEnvelope>>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Gets the highest stored event version for the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the highest version, or <c>0</c> if the aggregate has no events.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregateId"/> is <see langword="null"/>.</exception>
    public Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateId);
        try
        {
            lock (_lockObject)
            {
                var maxVersion = _events
                    .Where(e => e.AggregateId == aggregateId)
                    .OrderByDescending(e => e.AggregateVersion)
                    .FirstOrDefault()?.AggregateVersion ?? 0;

                return Task.FromResult(Result<long>.Success(maxVersion));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<long>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves a page of events across all aggregates, ordered by creation time and then version.
    /// </summary>
    /// <param name="pageNumber">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The maximum number of events per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the requested page of event envelopes.</returns>
    public Task<Result<List<EventEnvelope>>> GetAllEventsAsync(int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                var events = _events
                    .OrderBy(e => e.CreatedAt)
                    .ThenBy(e => e.AggregateVersion)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult(Result<List<EventEnvelope>>.Success(events));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<List<EventEnvelope>>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves a page of events that belong to the specified partition, ordered by creation time and then version.
    /// </summary>
    /// <param name="partitionKey">The partition key to match.</param>
    /// <param name="pageNumber">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The maximum number of events per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the requested page of matching event envelopes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="partitionKey"/> is <see langword="null"/>.</exception>
    public Task<Result<List<EventEnvelope>>> GetEventsByPartitionKeyAsync(string partitionKey, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(partitionKey);
        try
        {
            lock (_lockObject)
            {
                var events = _events
                    .Where(e => e.PartitionKey == partitionKey)
                    .OrderBy(e => e.CreatedAt)
                    .ThenBy(e => e.AggregateVersion)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult(Result<List<EventEnvelope>>.Success(events));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<List<EventEnvelope>>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Removes the stored events of the specified aggregate whose version is below <paramref name="beforeVersion"/>.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="beforeVersion">The exclusive upper version bound; events below this version are removed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the number of events removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregateId"/> is <see langword="null"/>.</exception>
    public Task<Result<int>> DeleteEventsBeforeVersionAsync(string aggregateId, long beforeVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateId);
        try
        {
            lock (_lockObject)
            {
                var toRemove = _events
                    .Where(e => e.AggregateId == aggregateId && e.AggregateVersion < beforeVersion)
                    .ToList();

                foreach (var e in toRemove)
                {
                    _events.Remove(e);
                }

                return Task.FromResult(Result<int>.Success(toRemove.Count));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<int>.Failure("DELETE_FAILED", ex.Message));
        }
    }
}
