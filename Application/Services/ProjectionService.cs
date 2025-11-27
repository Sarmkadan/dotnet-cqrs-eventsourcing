// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.Events;
using Microsoft.Extensions.Logging;
using Shared.Constants;
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

    public ProjectionService(IEventStore eventStore, ILogger<ProjectionService> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> UpdateProjectionAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
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

    public async Task<Result> RebuildProjectionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var streamResult = await _eventStore.GetEventStreamAsync(aggregateId, cancellationToken);
            if (!streamResult.IsSuccess)
                return Result.Failure(streamResult.ErrorCode!, streamResult.ErrorMessage!);

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

    public async Task<Result<Dictionary<string, object>>> GetProjectionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
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
