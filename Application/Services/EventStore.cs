// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.Events;
using Data.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Results;

/// <summary>
/// Event store implementation handling persistence, retrieval, and replay of domain events.
/// </summary>
public class EventStore : IEventStore
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventStore> _logger;

    public EventStore(IEventRepository eventRepository, ILogger<EventStore> logger)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> AppendEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        return await AppendEventsAsync(new List<DomainEvent> { @event }, cancellationToken);
    }

    public async Task<Result> AppendEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        try
        {
            var envelopes = events.Select(e =>
            {
                e.PopulateMetadata();
                return new EventEnvelope(e, SerializeEvent(e));
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appending events to event store");
            return Result.Failure("APPEND_FAILED", ex.Message);
        }
    }

    public async Task<Result<List<DomainEvent>>> GetEventStreamAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _eventRepository.GetEventsByAggregateIdAsync(aggregateId, cancellationToken);
            if (!result.IsSuccess)
                return Result<List<DomainEvent>>.Failure(result.ErrorCode!, result.ErrorMessage!);

            var domainEvents = DeserializeEvents(result.Data!);
            _logger.LogInformation("Retrieved event stream for aggregate {AggregateId} with {EventCount} events", aggregateId, domainEvents.Count);

            return Result<List<DomainEvent>>.Success(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event stream for aggregate {AggregateId}", aggregateId);
            return Result<List<DomainEvent>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    public async Task<Result<List<DomainEvent>>> GetEventStreamFromVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _eventRepository.GetEventsByAggregateIdAndVersionAsync(aggregateId, fromVersion, cancellationToken);
            if (!result.IsSuccess)
                return Result<List<DomainEvent>>.Failure(result.ErrorCode!, result.ErrorMessage!);

            var domainEvents = DeserializeEvents(result.Data!);
            return Result<List<DomainEvent>>.Success(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event stream from version {FromVersion}", fromVersion);
            return Result<List<DomainEvent>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    public async Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _eventRepository.GetAggregateVersionAsync(aggregateId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version for aggregate {AggregateId}", aggregateId);
            return Result<long>.Failure("VERSION_RETRIEVAL_FAILED", ex.Message);
        }
    }

    public async Task<Result> ReplayEventsAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var streamResult = await GetEventStreamAsync(aggregateId, cancellationToken);
            if (!streamResult.IsSuccess)
                return Result.Failure(streamResult.ErrorCode!, streamResult.ErrorMessage!);

            _logger.LogInformation("Replayed {EventCount} events for aggregate {AggregateId}", streamResult.Data!.Count, aggregateId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during event replay for aggregate {AggregateId}", aggregateId);
            return Result.Failure("REPLAY_FAILED", ex.Message);
        }
    }

    public async Task<Result<List<DomainEvent>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _eventRepository.GetEventsByTypeAsync(eventType, cancellationToken);
            if (!result.IsSuccess)
                return Result<List<DomainEvent>>.Failure(result.ErrorCode!, result.ErrorMessage!);

            var domainEvents = DeserializeEvents(result.Data!);
            return Result<List<DomainEvent>>.Success(domainEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events by type {EventType}", eventType);
            return Result<List<DomainEvent>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    public async Task<Result<int>> GetEventCountAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var streamResult = await GetEventStreamAsync(aggregateId, cancellationToken);
            if (!streamResult.IsSuccess)
                return Result<int>.Failure(streamResult.ErrorCode!, streamResult.ErrorMessage!);

            return Result<int>.Success(streamResult.Data!.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting events for aggregate {AggregateId}", aggregateId);
            return Result<int>.Failure("COUNT_FAILED", ex.Message);
        }
    }

    private List<DomainEvent> DeserializeEvents(List<EventEnvelope> envelopes)
    {
        var events = new List<DomainEvent>();
        foreach (var envelope in envelopes)
        {
            var @event = DeserializeEvent(envelope);
            if (@event is not null)
                events.Add(@event);
        }
        return events;
    }

    private DomainEvent? DeserializeEvent(EventEnvelope envelope)
    {
        try
        {
            return envelope.EventType switch
            {
                "AccountCreated" => System.Text.Json.JsonSerializer.Deserialize<AccountCreatedEvent>(envelope.EventData),
                "MoneyDeposited" => System.Text.Json.JsonSerializer.Deserialize<MoneyDepositedEvent>(envelope.EventData),
                "MoneyWithdrawn" => System.Text.Json.JsonSerializer.Deserialize<MoneyWithdrawnEvent>(envelope.EventData),
                "BalanceUpdated" => System.Text.Json.JsonSerializer.Deserialize<BalanceUpdatedEvent>(envelope.EventData),
                "AccountClosed" => System.Text.Json.JsonSerializer.Deserialize<AccountClosedEvent>(envelope.EventData),
                _ => null
            };
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
}
