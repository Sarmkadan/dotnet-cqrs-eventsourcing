#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Events;
using Microsoft.Extensions.Logging;
using Shared.Results;

/// <summary>
/// Projection service implementation for building and maintaining read models from events.
/// </summary>
public class ProjectionService : IProjectionService
{
    private readonly Dictionary<string, Dictionary<string, object>> _projections = new();
    private readonly IEventStore _eventStore;
    private readonly ILogger<ProjectionService> _logger;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionService"/> class.
    /// </summary>
    /// <param name="eventStore">Event store used to load event streams for rebuilding projections.</param>
    /// <param name="logger">Logger used to record projection activity.</param>
    public ProjectionService(IEventStore eventStore, ILogger<ProjectionService> logger)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(logger);

        _eventStore = eventStore;
        _logger = logger;
    }

    /// <summary>
    /// Applies a single domain event to the projection of its aggregate, creating the projection if needed.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the projection is updated, or a failure otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    public async Task<Result> UpdateProjectionAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        try
        {
            lock (_lockObject)
            {
                var projectionKey = @event.AggregateId;

                if (!_projections.ContainsKey(projectionKey))
                {
                    _projections[projectionKey] = InitializeProjection(@event.AggregateId, @event.AggregateType);
                }

                var projection = _projections[projectionKey];
                UpdateProjectionFromEvent(projection, @event);

                _logger.LogInformation("Updated projection for aggregate {AggregateId} with event {EventType}", @event.AggregateId, @event.GetEventType());
                return Result.Success();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating projection");
            return Result.Failure("UPDATE_PROJECTION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Rebuilds the projection for the specified aggregate by replaying its full event stream.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate whose projection should be rebuilt.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the projection is rebuilt, or a failure otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is null or whitespace.</exception>
    public async Task<Result> RebuildProjectionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateId);

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace.", nameof(aggregateId));
        }

        try
        {
            var streamResult = await _eventStore.GetEventStreamAsync(aggregateId, cancellationToken);
            if (!streamResult.IsSuccess)
            {
                return Result.Failure(streamResult.ErrorCode!, streamResult.ErrorMessage!);
            }

            lock (_lockObject)
            {
                _projections.Remove(aggregateId);

                foreach (var @event in streamResult.Data!)
                {
                    if (!_projections.ContainsKey(aggregateId))
                    {
                        _projections[aggregateId] = InitializeProjection(aggregateId, @event.AggregateType);
                    }

                    UpdateProjectionFromEvent(_projections[aggregateId], @event);
                }
            }

            _logger.LogInformation("Rebuilt projection for aggregate {AggregateId} from {EventCount} events", aggregateId, streamResult.Data!.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding projection for aggregate {AggregateId}", aggregateId);
            return Result.Failure("REBUILD_PROJECTION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Clears all existing projections and rebuilds them from the events in the event store.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the rebuild completes, or a failure otherwise.</returns>
    public async Task<Result> RebuildAllProjectionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                _projections.Clear();
            }

            var allEventsResult = await _eventStore.GetEventStreamAsync(string.Empty, cancellationToken);
            if (!allEventsResult.IsSuccess)
            {
                _logger.LogInformation("No events found to rebuild projections");
                return Result.Success();
            }

            var aggregateGroups = allEventsResult.Data!
                .GroupBy(e => e.AggregateId)
                .ToList();

            foreach (var group in aggregateGroups)
            {
                lock (_lockObject)
                {
                    var aggregateId = group.Key;
                    _projections[aggregateId] = InitializeProjection(aggregateId, group.First().AggregateType);

                    foreach (var @event in group.OrderBy(e => e.AggregateVersion))
                    {
                        UpdateProjectionFromEvent(_projections[aggregateId], @event);
                    }
                }
            }

            _logger.LogInformation("Rebuilt all {ProjectionCount} projections", aggregateGroups.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding all projections");
            return Result.Failure("REBUILD_ALL_PROJECTIONS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a copy of the projection for the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate whose projection is requested.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing a copy of the projection, or a failure if it does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is null or whitespace.</exception>
    public async Task<Result<Dictionary<string, object>>> GetProjectionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateId);

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace.", nameof(aggregateId));
        }

        try
        {
            lock (_lockObject)
            {
                if (_projections.TryGetValue(aggregateId, out var projection))
                {
                    return Result<Dictionary<string, object>>.Success(new Dictionary<string, object>(projection));
                }
            }

            return Result<Dictionary<string, object>>.Failure("PROJECTION_NOT_FOUND", $"Projection for aggregate {aggregateId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projection");
            return Result<Dictionary<string, object>>.Failure("GET_PROJECTION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Retrieves copies of all currently maintained projections.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing a copy of every projection.</returns>
    public async Task<Result<List<Dictionary<string, object>>>> GetAllProjectionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                var projections = _projections.Values.Select(p => new Dictionary<string, object>(p)).ToList();
                return Result<List<Dictionary<string, object>>>.Success(projections);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all projections");
            return Result<List<Dictionary<string, object>>>.Failure("GET_PROJECTIONS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Builds an account projection summary by replaying the event stream of the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate to summarize.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The summary built from the stream, or an empty summary if no stream is found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is null or whitespace.</exception>
    public async Task<AccountProjectionSummary> BuildProjectionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateId);

        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace.", nameof(aggregateId));
        }

        var summary = new AccountProjectionSummary();

        var streamResult = await _eventStore.GetEventStreamAsync(aggregateId, cancellationToken);
        if (!streamResult.IsSuccess || streamResult.Data is null)
        {
            _logger.LogWarning("No event stream found for aggregate {AggregateId}; returning empty summary", aggregateId);
            return summary;
        }

        foreach (var @event in streamResult.Data.OrderBy(e => e.AggregateVersion))
        {
            switch (@event)
            {
                case AccountCreatedEvent accountCreated:
                    summary.CurrentBalance = accountCreated.InitialBalance;
                    summary.Status = "Active";
                    break;

                case MoneyDepositedEvent moneyDeposited:
                    summary.CurrentBalance += moneyDeposited.Amount;
                    summary.TotalDeposits += moneyDeposited.Amount;
                    summary.TransactionCount++;
                    break;

                case MoneyWithdrawnEvent moneyWithdrawn:
                    summary.CurrentBalance -= moneyWithdrawn.Amount;
                    summary.TotalWithdrawals += moneyWithdrawn.Amount;
                    summary.TransactionCount++;
                    break;

                case BalanceUpdatedEvent balanceUpdated:
                    summary.CurrentBalance = balanceUpdated.NewBalance;
                    break;

                case AccountClosedEvent:
                    summary.Status = "Closed";
                    break;
            }

            summary.LastUpdated = @event.OccurredAt;
        }

        _logger.LogInformation("Built projection summary for aggregate {AggregateId} from {EventCount} events", aggregateId, streamResult.Data.Count);
        return summary;
    }

    private Dictionary<string, object> InitializeProjection(string aggregateId, string aggregateType)
    {
        return new Dictionary<string, object>
        {
            { "AggregateId", aggregateId },
            { "AggregateType", aggregateType },
            { "Version", 0L },
            { "CreatedAt", DateTime.UtcNow },
            { "UpdatedAt", DateTime.UtcNow }
        };
    }

    private void UpdateProjectionFromEvent(Dictionary<string, object> projection, DomainEvent @event)
    {
        projection["Version"] = @event.AggregateVersion;
        projection["UpdatedAt"] = @event.OccurredAt;

        switch (@event)
        {
            case AccountCreatedEvent accountCreated:
                projection["AccountNumber"] = accountCreated.AccountNumber;
                projection["AccountHolder"] = accountCreated.AccountHolder;
                projection["Currency"] = accountCreated.Currency;
                projection["InitialBalance"] = accountCreated.InitialBalance;
                projection["Status"] = "Active";
                break;

            case MoneyDepositedEvent moneyDeposited:
                projection["LastTransactionType"] = "Deposit";
                projection["LastTransactionAmount"] = moneyDeposited.Amount;
                projection["LastTransactionDate"] = moneyDeposited.ProcessedAt;
                break;

            case MoneyWithdrawnEvent moneyWithdrawn:
                projection["LastTransactionType"] = "Withdrawal";
                projection["LastTransactionAmount"] = moneyWithdrawn.Amount;
                projection["LastTransactionDate"] = moneyWithdrawn.ProcessedAt;
                break;

            case AccountClosedEvent accountClosed:
                projection["Status"] = "Closed";
                projection["CloseDate"] = accountClosed.OccurredAt;
                projection["ClosingBalance"] = accountClosed.ClosingBalance;
                break;
        }
    }
}
