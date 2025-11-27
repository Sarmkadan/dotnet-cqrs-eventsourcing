// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using Domain.Events;
using Shared.Exceptions;
using Shared.Results;

/// <summary>
/// In-memory implementation of event repository for testing and development.
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private readonly List<EventEnvelope> _events = new();
    private readonly object _lockObject = new();

    public Task<Result> SaveEventAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default)
    {
        return SaveEventsAsync(new List<EventEnvelope> { eventEnvelope }, cancellationToken);
    }

    public Task<Result> SaveEventsAsync(List<EventEnvelope> envelopes, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                foreach (var envelope in envelopes)
                {
                    // Check for optimistic concurrency
                    var aggregateEvents = _events.Where(e => e.AggregateId == envelope.AggregateId).ToList();
                    var maxVersion = aggregateEvents.Count > 0 ? aggregateEvents.Max(e => e.AggregateVersion) : 0;

                    if (envelope.AggregateVersion <= maxVersion)
                        return Task.FromResult(Result.Failure(
                            "CONCURRENCY_CONFLICT",
                            $"Concurrency conflict for aggregate {envelope.AggregateId}. Expected version > {maxVersion}, got {envelope.AggregateVersion}"
                        ));

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

    public Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
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

    public Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAndVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default)
    {
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

    public Task<Result<EventEnvelope>> GetEventByIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                var @event = _events.FirstOrDefault(e => e.Id == eventId);
                if (@event is null)
                    return Task.FromResult(Result<EventEnvelope>.Failure("NOT_FOUND", $"Event {eventId} not found"));

                return Task.FromResult(Result<EventEnvelope>.Success(@event));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<EventEnvelope>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    public Task<Result<List<EventEnvelope>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                var events = _events.Where(e => e.EventType == eventType).OrderBy(e => e.CreatedAt).ToList();
                return Task.FromResult(Result<List<EventEnvelope>>.Success(events));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<List<EventEnvelope>>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }

    public Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
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

    public Task<Result<List<EventEnvelope>>> GetAllEventsAsync(int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                var events = _events
                    .OrderBy(e => e.CreatedAt)
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
}
