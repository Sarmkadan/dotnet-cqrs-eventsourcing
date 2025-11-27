// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using Domain.AggregateRoots;
using Domain.Events;
using System.Text.Json;
using Shared.Exceptions;
using Shared.Results;

/// <summary>
/// Repository for Account aggregates using event sourcing pattern.
/// </summary>
public class AccountRepository : IRepository<Account>
{
    private readonly IEventRepository _eventRepository;
    private readonly Dictionary<string, Account> _accounts = new(); // Cache for consistency

    public AccountRepository(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
    }

    public async Task<Result<Account>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            if (_accounts.TryGetValue(id, out var cachedAccount))
                return Result<Account>.Success(cachedAccount);

            // Get events from event store
            var eventsResult = await _eventRepository.GetEventsByAggregateIdAsync(id, cancellationToken);
            eventsResult.ThrowIfFailure();

            if (eventsResult.Data!.Count == 0)
                return Result<Account>.Failure("NOT_FOUND", $"Account {id} not found");

            // Reconstruct account from events
            var account = new Account(id);
            var domainEvents = DeserializeEvents(eventsResult.Data);
            account.LoadFromHistory(domainEvents);

            _accounts[id] = account;
            return Result<Account>.Success(account);
        }
        catch (Exception ex)
        {
            return Result<Account>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    public async Task<Result> SaveAsync(Account aggregate, CancellationToken cancellationToken = default)
    {
        try
        {
            var uncommittedEvents = aggregate.GetUncommittedEvents();
            if (uncommittedEvents.Count == 0)
                return Result.Success();

            // Serialize and wrap events
            var envelopes = uncommittedEvents
                .Select(e => new EventEnvelope(e, SerializeEvent(e)))
                .ToList();

            // Persist to event store
            var saveResult = await _eventRepository.SaveEventsAsync(envelopes, cancellationToken);
            if (!saveResult.IsSuccess)
                return saveResult;

            aggregate.ClearUncommittedEvents();
            _accounts[aggregate.Id] = aggregate;

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("SAVE_FAILED", ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _accounts.Remove(id);
            // Note: In event sourcing, we don't delete events; we handle deletion through aggregate logic
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("DELETE_FAILED", ex.Message);
        }
    }

    public async Task<Result<List<Account>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allEventsResult = await _eventRepository.GetAllEventsAsync(1, 10000, cancellationToken);
            allEventsResult.ThrowIfFailure();

            var accountIds = allEventsResult.Data!
                .GroupBy(e => e.AggregateId)
                .Select(g => g.Key)
                .ToList();

            var accounts = new List<Account>();
            foreach (var accountId in accountIds)
            {
                var result = await GetByIdAsync(accountId, cancellationToken);
                if (result.IsSuccess)
                    accounts.Add(result.Data!);
            }

            return Result<List<Account>>.Success(accounts);
        }
        catch (Exception ex)
        {
            return Result<List<Account>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await GetByIdAsync(id, cancellationToken);
        return result.IsSuccess;
    }

    // Deserialize event envelopes back to domain events
    private List<DomainEvent> DeserializeEvents(List<EventEnvelope> envelopes)
    {
        var events = new List<DomainEvent>();

        foreach (var envelope in envelopes)
        {
            DomainEvent? domainEvent = envelope.EventType switch
            {
                "AccountCreated" => JsonSerializer.Deserialize<AccountCreatedEvent>(envelope.EventData),
                "MoneyDeposited" => JsonSerializer.Deserialize<MoneyDepositedEvent>(envelope.EventData),
                "MoneyWithdrawn" => JsonSerializer.Deserialize<MoneyWithdrawnEvent>(envelope.EventData),
                "BalanceUpdated" => JsonSerializer.Deserialize<BalanceUpdatedEvent>(envelope.EventData),
                "AccountClosed" => JsonSerializer.Deserialize<AccountClosedEvent>(envelope.EventData),
                _ => throw new DomainException($"Unknown event type: {envelope.EventType}", "UNKNOWN_EVENT_TYPE")
            };

            if (domainEvent is not null)
                events.Add(domainEvent);
        }

        return events;
    }

    // Serialize domain events to JSON
    private string SerializeEvent(DomainEvent @event)
    {
        return JsonSerializer.Serialize(@event, @event.GetType());
    }
}
