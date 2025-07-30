#nullable enable

namespace DotNetCqrsEventSourcing.Data.Repositories;

using Domain.Events;
using Shared.Results;

/// <summary>
/// Extension methods for <see cref="InMemoryEventRepository"/> providing common repository operations.
/// </summary>
public static class InMemoryEventRepositoryExtensions
{
    /// <summary>
    /// Gets the first event for a specific aggregate.
    /// </summary>
    /// <param name="repository">The event repository</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first event envelope or failure result</returns>
    public static async Task<Result<EventEnvelope>> GetFirstEventAsync(
        this InMemoryEventRepository repository,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetEventsByAggregateIdAsync(aggregateId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Result<EventEnvelope>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var events = result.Data;
        return events is { Count: > 0 }
            ? Result<EventEnvelope>.Success(events[0])
            : Result<EventEnvelope>.Failure("NOT_FOUND", $"No events found for aggregate {aggregateId}");
    }

    /// <summary>
    /// Gets the last event for a specific aggregate.
    /// </summary>
    /// <param name="repository">The event repository</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The last event envelope or failure result</returns>
    public static async Task<Result<EventEnvelope>> GetLastEventAsync(
        this InMemoryEventRepository repository,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetEventsByAggregateIdAsync(aggregateId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Result<EventEnvelope>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var events = result.Data;
        return events is { Count: > 0 }
            ? Result<EventEnvelope>.Success(events[^1])
            : Result<EventEnvelope>.Failure("NOT_FOUND", $"No events found for aggregate {aggregateId}");
    }

    /// <summary>
    /// Checks if an aggregate exists in the repository.
    /// </summary>
    /// <param name="repository">The event repository</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if aggregate exists, false otherwise</returns>
    public static async Task<Result<bool>> AggregateExistsAsync(
        this InMemoryEventRepository repository,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAggregateVersionAsync(aggregateId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Result<bool>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        return Result<bool>.Success(result.Data > 0);
    }

    /// <summary>
    /// Gets the count of events for a specific aggregate.
    /// </summary>
    /// <param name="repository">The event repository</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of events or failure result</returns>
    public static async Task<Result<int>> GetEventCountAsync(
        this InMemoryEventRepository repository,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetEventsByAggregateIdAsync(aggregateId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Result<int>.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        return Result<int>.Success(result.Data?.Count ?? 0);
    }
}
