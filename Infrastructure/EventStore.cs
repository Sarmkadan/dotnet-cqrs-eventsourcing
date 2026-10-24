using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.Infrastructure;

/// <summary>
/// Event store implementation for managing event persistence and retrieval.
/// Provides basic event store functionality with in-memory storage.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends a domain event to the stream of the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate whose stream receives the event.</param>
    /// <param name="event">The domain event to append.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the event is appended, or a failure otherwise.</returns>
    Task<Result> AppendEventAsync(string aggregateId, DomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the events recorded for the specified aggregate, in append order.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate whose events are requested.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing a copy of the aggregate's event stream, or a failure if retrieval fails.</returns>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="logger">Logger used to record event store activity.</param>
    public EventStore(ILogger<EventStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Appends a domain event to the in-memory stream of the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate whose stream receives the event.</param>
    /// <param name="event">The domain event to append.</param>
    /// <param name="cancellationToken">Token to cancel the operation (not used by the in-memory implementation).</param>
    /// <returns>A completed task containing a success result, or a failure if the append fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Returns a snapshot copy of the in-memory event stream for the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate whose events are requested.</param>
    /// <param name="cancellationToken">Token to cancel the operation (not used by the in-memory implementation).</param>
    /// <returns>A result containing the events, or an empty list if the aggregate has no stream.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is null, empty, or whitespace.</exception>
    public Task<Result<List<DomainEvent>>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        try
        {
            List<DomainEvent>? snapshot = null;
            lock (_sync)
            {
                if (_eventStreams.TryGetValue(aggregateId, out var stream))
                {
                    snapshot = new List<DomainEvent>(stream);
                }
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
