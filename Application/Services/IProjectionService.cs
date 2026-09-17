#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.Events;
using Shared.Results;

/// <summary>
/// Strongly-typed account projection summary computed directly from an aggregate's
/// event stream. Serves as a compact read model for reporting scenarios.
/// </summary>
public sealed class AccountProjectionSummary
{
    /// <summary>Lifecycle status of the account: "Active" or "Closed".</summary>
    public string Status { get; set; } = "Active";

    /// <summary>Current balance derived from replaying all monetary events.</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Sum of all deposited amounts, excluding the initial balance.</summary>
    public decimal TotalDeposits { get; set; }

    /// <summary>Sum of all withdrawn amounts.</summary>
    public decimal TotalWithdrawals { get; set; }

    /// <summary>Number of monetary transactions (deposits and withdrawals).</summary>
    public int TransactionCount { get; set; }

    /// <summary>UTC timestamp of the most recent event applied to this summary.</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"AccountProjectionSummary {{ Status = {Status}, CurrentBalance = {CurrentBalance}, TotalDeposits = {TotalDeposits}, TotalWithdrawals = {TotalWithdrawals}, TransactionCount = {TransactionCount}, LastUpdated = {LastUpdated} }}";
    }
}

/// <summary>
/// Projection service interface for building and maintaining read models from events.
/// </summary>
public interface IProjectionService
{
    /// <summary>
    /// Applies a single domain event to the corresponding read model.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating whether the projection was updated successfully.</returns>
    Task<Result> UpdateProjectionAsync(DomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the read model for a single aggregate by replaying its event stream.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to rebuild.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating whether the projection was rebuilt successfully.</returns>
    Task<Result> RebuildProjectionAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the read models for all aggregates by replaying their event streams.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating whether all projections were rebuilt successfully.</returns>
    Task<Result> RebuildAllProjectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current projection for a single aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the projection data, or a failure if it does not exist.</returns>
    Task<Result<Dictionary<string, object>>> GetProjectionAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the projections for all aggregates.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing all projection data.</returns>
    Task<Result<List<Dictionary<string, object>>>> GetAllProjectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a strongly-typed <see cref="AccountProjectionSummary"/> for the given
    /// aggregate by replaying its full event stream.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to project.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<AccountProjectionSummary> BuildProjectionAsync(string aggregateId, CancellationToken cancellationToken = default);
}
