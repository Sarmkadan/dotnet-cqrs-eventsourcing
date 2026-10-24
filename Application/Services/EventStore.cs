#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using System.Text;
using Data.Repositories;
using Domain.Events;
using Infrastructure.Events;
using Upcasting;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Results;

/// <summary>
/// Event store implementation handling persistence, retrieval, and replay of domain events.
/// </summary>
public class EventStore : IEventStore
{
    /// <summary>
    /// Default upper bound on a serialized event payload, in UTF-8 bytes (1 MiB).
    /// </summary>
    public const int DefaultMaxEventPayloadBytes = 1024 * 1024;

    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventStore> _logger;
    private readonly EventTypeRegistry? _eventTypeRegistry;
    private readonly EventUpcastingPipeline? _upcastingPipeline;
    private readonly int _maxEventPayloadBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventRepository">The event repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="eventTypeRegistry">The event type registry (optional).</param>
    /// <param name="upcastingPipeline">
    /// The pipeline that upgrades stored payloads to the current schema version before deserialization (optional).
    /// When <see langword="null"/>, payloads are deserialized exactly as stored.
    /// </param>
    /// <param name="maxEventPayloadBytes">
    /// The maximum size of an event payload in UTF-8 bytes, enforced when events are appended and before
    /// a stored payload is parsed or deserialized. Defaults to <see cref="DefaultMaxEventPayloadBytes"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventRepository"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxEventPayloadBytes"/> is zero or negative.</exception>
    public EventStore(
        IEventRepository eventRepository,
        ILogger<EventStore> logger,
        EventTypeRegistry? eventTypeRegistry = null,
        EventUpcastingPipeline? upcastingPipeline = null,
        int maxEventPayloadBytes = DefaultMaxEventPayloadBytes)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEventPayloadBytes);
        _eventTypeRegistry = eventTypeRegistry;
        _upcastingPipeline = upcastingPipeline;
        _maxEventPayloadBytes = maxEventPayloadBytes;
    }

    /// <summary>
    /// Appends a single domain event to the event store.
    /// </summary>
    /// <param name="event">The domain event to append.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> AppendEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        return await AppendEventsAsync(new List<DomainEvent> { @event }, cancellationToken);
    }

    /// <summary>
    /// Appends multiple domain events to the event store.
    /// </summary>
    /// <param name="events">The domain events to append.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> AppendEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        try
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (events.Count == 0)
            {
                throw new ValidationException("Event list cannot be empty.")
                    .WithError(nameof(events), "At least one event is required");
            }

            if (events.Any(e => e == null))
            {
                throw new ValidationException("Event list cannot contain null events.")
                    .WithError(nameof(events), "Null events are not allowed");
            }

            var envelopes = events.Select(e =>
            {
                e.PopulateMetadata();
                var envelope = new EventEnvelope(e, SerializeEvent(e));
                // Reject here so an event that could never be read back within the limit is not persisted.
                EnsurePayloadWithinLimit(envelope.EventType, envelope.EventData);
                return envelope;
            }).ToList();

            var result = await _eventRepository.SaveEventsAsync(envelopes, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully appended {EventCount} events to event store", events.Count);
            }
            else
            {
                _logger.LogError("Failed to append events: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Null argument passed to AppendEventsAsync");
            throw new ValidationException("Null argument passed to AppendEventsAsync", ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending events to event store");
            return Result.Failure("APPEND_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Retrieves the event stream for a given aggregate ID.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of domain events or an error.</returns>
    public async Task<Result<List<DomainEvent>>> GetEventStreamAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
            {
                throw new ValidationException("Aggregate ID cannot be null or whitespace.")
                    .WithError(nameof(aggregateId), "Aggregate ID is required");
            }

            var result = await _eventRepository.GetEventsByAggregateIdAsync(aggregateId, cancellationToken);
            if (!result.IsSuccess)
            {
                return Result<List<DomainEvent>>.Failure(result.ErrorCode!, result.ErrorMessage!);
            }

            var domainEvents = DeserializeEvents(result.Data!);
            _logger.LogInformation("Retrieved event stream for aggregate {AggregateId} with {EventCount} events", aggregateId, domainEvents.Count);

            return Result<List<DomainEvent>>.Success(domainEvents);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event stream for aggregate {AggregateId}", aggregateId);
            return Result<List<DomainEvent>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Retrieves the event stream for a given aggregate ID starting from a specific version.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="fromVersion">The version to start from (exclusive).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of domain events or an error.</returns>
    public async Task<Result<List<DomainEvent>>> GetEventStreamFromVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
            {
                throw new ValidationException("Aggregate ID cannot be null or whitespace.")
                    .WithError(nameof(aggregateId), "Aggregate ID is required");
            }

            if (fromVersion <= 0)
            {
                throw new ValidationException("From version must be positive.")
                    .WithError(nameof(fromVersion), "From version must be greater than zero");
            }

            var result = await _eventRepository.GetEventsByAggregateIdAndVersionAsync(aggregateId, fromVersion, cancellationToken);
            if (!result.IsSuccess)
            {
                return Result<List<DomainEvent>>.Failure(result.ErrorCode!, result.ErrorMessage!);
            }

            var domainEvents = DeserializeEvents(result.Data!);
            return Result<List<DomainEvent>>.Success(domainEvents);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event stream from version {FromVersion}", fromVersion);
            return Result<List<DomainEvent>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets the current version of an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the aggregate version or an error.</returns>
    public async Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
            {
                throw new ValidationException("Aggregate ID cannot be null or whitespace.")
                    .WithError(nameof(aggregateId), "Aggregate ID is required");
            }

            return await _eventRepository.GetAggregateVersionAsync(aggregateId, cancellationToken);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version for aggregate {AggregateId}", aggregateId);
            return Result<long>.Failure("VERSION_RETRIEVAL_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Replays all events for a given aggregate ID.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> ReplayEventsAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
            {
                throw new ValidationException("Aggregate ID cannot be null or whitespace.")
                    .WithError(nameof(aggregateId), "Aggregate ID is required");
            }

            var streamResult = await GetEventStreamAsync(aggregateId, cancellationToken);
            if (!streamResult.IsSuccess)
            {
                return Result.Failure(streamResult.ErrorCode!, streamResult.ErrorMessage!);
            }

            _logger.LogInformation("Replayed {EventCount} events for aggregate {AggregateId}", streamResult.Data!.Count, aggregateId);
            return Result.Success();
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during event replay for aggregate {AggregateId}", aggregateId);
            return Result.Failure("REPLAY_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Retrieves events by their type.
    /// </summary>
    /// <param name="eventType">The type of the event to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of domain events or an error.</returns>
    public async Task<Result<List<DomainEvent>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _eventRepository.GetEventsByTypeAsync(eventType, cancellationToken);
            if (!result.IsSuccess)
            {
                return Result<List<DomainEvent>>.Failure(result.ErrorCode!, result.ErrorMessage!);
            }

            var domainEvents = DeserializeEvents(result.Data!);
            return Result<List<DomainEvent>>.Success(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events by type {EventType}", eventType);
            return Result<List<DomainEvent>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets the count of events for a given aggregate ID.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the event count or an error.</returns>
    public async Task<Result<int>> GetEventCountAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var streamResult = await GetEventStreamAsync(aggregateId, cancellationToken);
            if (!streamResult.IsSuccess)
            {
                return Result<int>.Failure(streamResult.ErrorCode!, streamResult.ErrorMessage!);
            }

            return Result<int>.Success(streamResult.Data!.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting events for aggregate {AggregateId}", aggregateId);
            return Result<int>.Failure("COUNT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Retrieves events by their partition key with pagination.
    /// </summary>
    /// <param name="partitionKey">The partition key to filter events by.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of events per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the list of domain events or an error.</returns>
    public async Task<Result<List<DomainEvent>>> GetEventsByPartitionKeyAsync(string partitionKey, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _eventRepository.GetEventsByPartitionKeyAsync(partitionKey, pageNumber, pageSize, cancellationToken);
            if (!result.IsSuccess)
            {
                return Result<List<DomainEvent>>.Failure(result.ErrorCode!, result.ErrorMessage!);
            }

            var domainEvents = DeserializeEvents(result.Data!);
            _logger.LogInformation(
                "Retrieved {EventCount} event(s) for partition key '{PartitionKey}' (page {Page})",
                domainEvents.Count, partitionKey, pageNumber);

            return Result<List<DomainEvent>>.Success(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for partition key '{PartitionKey}'", partitionKey);
            return Result<List<DomainEvent>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    private List<DomainEvent> DeserializeEvents(List<EventEnvelope> envelopes)
    {
        var events = new List<DomainEvent>();
        foreach (var envelope in envelopes)
        {
            var @event = DeserializeEvent(envelope);
            if (@event is not null)
            {
                events.Add(@event);
            }
        }
        return events;
    }

    private DomainEvent? DeserializeEvent(EventEnvelope envelope)
    {
        try
        {
            // Strict allow-listing: only registered event types can be deserialized.
            // UnknownEventTypeException is thrown for unregistered types, preventing
            // deserialization gadget vectors.
            if (_eventTypeRegistry is null)
            {
                _logger.LogWarning("EventTypeRegistry is not configured. Cannot deserialize event type '{EventType}'.", envelope.EventType);
                return null;
            }

            var resolvedType = _eventTypeRegistry.Resolve(envelope.EventType);
            EnsurePayloadWithinLimit(envelope.EventType, envelope.EventData);
            var payload = _upcastingPipeline is null
                ? envelope.EventData
                : _upcastingPipeline.UpcastToCurrentVersion(envelope);
            return (DomainEvent?)System.Text.Json.JsonSerializer.Deserialize(payload, resolvedType);
        }
        catch (UnknownEventTypeException ex)
        {
            _logger.LogError(ex, "Unknown event type '{EventType}' attempted for deserialization. This is a security violation.", envelope.EventType);
            return null;
        }
        catch (EventPayloadTooLargeException)
        {
            // Propagate instead of dropping the event: a silently skipped event would corrupt the replayed aggregate state.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing event of type {EventType}", envelope.EventType);
            return null;
        }
    }

    private string SerializeEvent(DomainEvent @event)
    {
        return System.Text.Json.JsonSerializer.Serialize(@event, @event.GetType());
    }

    /// <summary>
    /// Ensures a serialized payload fits within the configured maximum size, measured in UTF-8 bytes.
    /// </summary>
    /// <param name="eventType">The event type name the payload belongs to; used in the exception message.</param>
    /// <param name="payload">The serialized payload to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <see langword="null"/>.</exception>
    /// <exception cref="EventPayloadTooLargeException">Thrown when the payload exceeds the configured maximum size.</exception>
    private void EnsurePayloadWithinLimit(string eventType, string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var payloadBytes = Encoding.UTF8.GetByteCount(payload);
        if (payloadBytes > _maxEventPayloadBytes)
        {
            throw new EventPayloadTooLargeException(eventType, payloadBytes, _maxEventPayloadBytes);
        }
    }
}