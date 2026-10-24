#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using System.Text.Json;
using Domain.AggregateRoots;
using Domain.Events;
using Shared.Exceptions;
using Shared.Results;

/// <summary>
/// Repository for Account aggregates using event sourcing pattern.
/// </summary>
public class AccountRepository : IRepository<Account>
{
    private readonly IEventRepository _eventRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository"/> class.
    /// </summary>
    /// <param name="eventRepository">Event repository used to read and append account events.</param>
    public AccountRepository(IEventRepository eventRepository)
    {
        ArgumentNullException.ThrowIfNull(eventRepository);

        _eventRepository = eventRepository;
    }

    /// <summary>
    /// Rehydrates the account with the specified identifier from its event stream.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the rehydrated account, or a failure if it does not exist or cannot be loaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see langword="null"/>.</exception>
    public async Task<Result<Account>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        try
        {
            // Always rehydrate from the event stream. Caching aggregate instances here
            // would hand the same mutable object to concurrent callers and serve stale
            // state whenever another writer appends events to the stream.

            // Get events from event store
            var eventsResult = await _eventRepository.GetEventsByAggregateIdAsync(id, cancellationToken);
            eventsResult.ThrowIfFailure();

            if (eventsResult.Data!.Count == 0)
            {
                return Result<Account>.Failure("NOT_FOUND", $"Account {id} not found");
            }

            // Reconstruct account from events
            var account = new Account(id);
            var domainEvents = DeserializeEvents(eventsResult.Data);
            account.LoadFromHistory(domainEvents);

            return Result<Account>.Success(account);
        }
        catch (Exception ex)
        {
            return Result<Account>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Appends the uncommitted events of the account to the event store and clears them on success.
    /// </summary>
    /// <param name="aggregate">The account whose uncommitted events should be persisted.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the events are persisted or there is nothing to save, or a failure otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is <see langword="null"/>.</exception>
    public async Task<Result> SaveAsync(Account aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        try
        {
            var uncommittedEvents = aggregate.GetUncommittedEvents();
            if (uncommittedEvents.Count == 0)
            {
                return Result.Success();
            }

            // Serialize and wrap events
            var envelopes = uncommittedEvents
                .Select(e => new EventEnvelope(e, SerializeEvent(e)))
                .ToList();

            // Persist to event store
            var saveResult = await _eventRepository.SaveEventsAsync(envelopes, cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            aggregate.ClearUncommittedEvents();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("SAVE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Reports a deletion request for the account. Event streams are never removed, so this always succeeds without modifying stored events.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation (not used).</param>
    /// <returns>A completed task containing a success result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see langword="null"/>.</exception>
    public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        // Note: In event sourcing, we don't delete events; we handle deletion through aggregate logic
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Rehydrates all accounts found in the event store.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the accounts that could be rehydrated, or a failure if event retrieval fails.</returns>
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
                {
                    accounts.Add(result.Data!);
                }
            }

            return Result<List<Account>>.Success(accounts);
        }
        catch (Exception ex)
        {
            return Result<List<Account>>.Failure("RETRIEVE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Determines whether an account with the specified identifier can be loaded.
    /// </summary>
    /// <param name="id">The account identifier to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the account exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see langword="null"/>.</exception>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

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
            {
                events.Add(domainEvent);
            }
        }

        return events;
    }

    // Serialize domain events to JSON
    private string SerializeEvent(DomainEvent @event)
    {
        return JsonSerializer.Serialize(@event, @event.GetType());
    }
}
