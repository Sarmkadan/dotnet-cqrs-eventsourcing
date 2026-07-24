using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.Infrastructure;

/// <summary>
/// Event store implementation for managing event persistence and retrieval.
/// Provides basic event store functionality with in-memory storage.
/// </summary>
public interface IEventStore
{
    Task<Result> AppendEventAsync(string aggregateId, DomainEvent @event, CancellationToken cancellationToken = default);
    Task<Result<List<DomainEvent>>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Basic in-memory event store implementation.
/// Thread-safe: appends and reads are synchronized, and readers receive a
/// snapshot copy of the stream so callers can never mutate internal state.
/// </summary>
public class EventStore : IEventStore
{
    private readonly Dictionary<string, List<DomainEvent>> _eventStreams = new();
    private readonly object _sync = new();
    private readonly ILogger<EventStore> _logger;

    public EventStore(ILogger<EventStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result> AppendEventAsync(string aggregateId, DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentNullException.ThrowIfNull(@event);

        try
        {
            lock (_sync)
            {
                if (!_eventStreams.TryGetValue(aggregateId, out var stream))
                {
                    stream = new List<DomainEvent>();
                    _eventStreams[aggregateId] = stream;
                }

                stream.Add(@event);
            }

            _logger.LogInformation("Event appended to stream: {AggregateId} - {EventType} (v{Version})",
                aggregateId, @event.GetType().Name, @event.AggregateVersion);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending event to stream {AggregateId}", aggregateId);
            return Task.FromResult(Result.Failure("APPEND_FAILED", ex.Message));
        }
    }

    public Task<Result<List<DomainEvent>>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        try
        {
            List<DomainEvent>? snapshot = null;
            lock (_sync)
            {
                if (_eventStreams.TryGetValue(aggregateId, out var stream))
                    snapshot = new List<DomainEvent>(stream);
            }

            if (snapshot is not null)
            {
                _logger.LogInformation("Retrieved {EventCount} events for aggregate {AggregateId}", snapshot.Count, aggregateId);
                return Task.FromResult(Result<List<DomainEvent>>.Success(snapshot));
            }

            _logger.LogDebug("No events found for aggregate {AggregateId}", aggregateId);
            return Task.FromResult(Result<List<DomainEvent>>.Success(new List<DomainEvent>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for aggregate {AggregateId}", aggregateId);
            return Task.FromResult(Result<List<DomainEvent>>.Failure("RETRIEVE_FAILED", ex.Message));
        }
    }
}
