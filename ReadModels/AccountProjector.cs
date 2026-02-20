#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Domain.Events;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Projects account-related domain events onto <see cref="AccountReadModel"/> materialized views,
/// handling the full account lifecycle: creation, deposits, withdrawals, balance adjustments,
/// and permanent closure.
/// </summary>
/// <remarks>
/// <para>
/// This projector is designed to be idempotent when events arrive in aggregate-version order.
/// If an event arrives whose <see cref="DomainEvent.AggregateVersion"/> is less than or equal
/// to <see cref="AccountReadModel.ProjectedVersion"/> the event is silently skipped, preserving
/// the read model's current state.
/// </para>
/// <para>
/// The projector mutates the <paramref name="current"/> instance in place when one exists,
/// avoiding unnecessary object allocations. For thread-safety under concurrent event delivery,
/// configure <see cref="ReadModelProjectionOptions.MaxConcurrentProjectors"/> to <c>1</c>
/// or use a database-backed store with optimistic concurrency.
/// </para>
/// </remarks>
public sealed class AccountProjector : IReadModelProjector<AccountReadModel>
{
    private readonly ILogger<AccountProjector> _logger;

    /// <summary>Initializes a new <see cref="AccountProjector"/>.</summary>
    public AccountProjector(ILogger<AccountProjector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The name is intentionally versioned (<c>account-v1</c>) so that a breaking change
    /// to the read model shape can be rolled out as a new projection alongside the old one
    /// without disturbing existing consumers.
    /// </remarks>
    public string ProjectionName => "account-v1";

    /// <inheritdoc />
    public bool CanProject(DomainEvent @event) => @event is
        AccountCreatedEvent or
        MoneyDepositedEvent or
        MoneyWithdrawnEvent or
        BalanceUpdatedEvent or
        AccountClosedEvent;

    /// <inheritdoc />
    public string GetKey(DomainEvent @event) => @event.AggregateId;

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see langword="null"/> only when an <see cref="AccountClosedEvent"/> or another
    /// event arrives for an aggregate that was never created — callers interpret a <see langword="null"/>
    /// return as a signal to delete the store entry.  In practice this branch is unreachable in
    /// a well-formed event stream; it is included as a defensive guard.
    /// </remarks>
    public Task<AccountReadModel?> ProjectAsync(
        DomainEvent @event,
        AccountReadModel? current,
        CancellationToken cancellationToken = default)
    {
        if (current is not null && @event.AggregateVersion <= current.ProjectedVersion)
        {
            _logger.LogDebug(
                "Skipping stale event {EventId} (v{EventVersion}) for account {AccountId}; " +
                "read model is already at v{CurrentVersion}.",
                @event.EventId, @event.AggregateVersion,
                @event.AggregateId, current.ProjectedVersion);

            return Task.FromResult<AccountReadModel?>(current);
        }

        var updated = @event switch
        {
            AccountCreatedEvent  e => ApplyCreated(e),
            MoneyDepositedEvent  e => ApplyDeposited(current, e),
            MoneyWithdrawnEvent  e => ApplyWithdrawn(current, e),
            BalanceUpdatedEvent  e => ApplyBalanceUpdated(current, e),
            AccountClosedEvent   e => ApplyClosed(current, e),
            _                      => current
        };

        if (updated is not null)
        {
            updated.ProjectedVersion = @event.AggregateVersion;
            updated.LastUpdatedAt    = DateTime.UtcNow;
        }

        return Task.FromResult(updated);
    }

    // -------------------------------------------------------------------------
    // Per-event application helpers
    // -------------------------------------------------------------------------

    private static AccountReadModel ApplyCreated(AccountCreatedEvent e) =>
        new()
        {
            AccountId      = e.AggregateId,
            AccountNumber  = e.AccountNumber,
            AccountHolder  = e.AccountHolder,
            Currency       = e.Currency,
            OpenedAt       = e.OccurredAt,
            CurrentBalance = e.InitialBalance,
            Status         = AccountReadModelStatus.Active,
            TotalDeposited = e.InitialBalance > 0m ? e.InitialBalance : 0m
        };

    private static AccountReadModel? ApplyDeposited(AccountReadModel? current, MoneyDepositedEvent e)
    {
        if (current is null)
            return null;

        current.CurrentBalance += e.Amount;
        current.TotalDeposited += e.Amount;
        current.Transactions.Add(new TransactionSummary(
            e.EventId, "Deposit", e.Amount,
            current.Currency, e.Reference, e.ProcessedAt));

        return current;
    }

    private static AccountReadModel? ApplyWithdrawn(AccountReadModel? current, MoneyWithdrawnEvent e)
    {
        if (current is null)
            return null;

        current.CurrentBalance -= e.Amount;
        current.TotalWithdrawn += e.Amount;
        current.Transactions.Add(new TransactionSummary(
            e.EventId, "Withdrawal", e.Amount,
            current.Currency, e.Reference, e.ProcessedAt));

        return current;
    }

    private static AccountReadModel? ApplyBalanceUpdated(AccountReadModel? current, BalanceUpdatedEvent e)
    {
        if (current is null)
            return null;

        current.CurrentBalance = e.NewBalance;
        return current;
    }

    private static AccountReadModel? ApplyClosed(AccountReadModel? current, AccountClosedEvent e)
    {
        if (current is null)
            return null;

        current.Status         = AccountReadModelStatus.Closed;
        current.ClosedAt       = e.OccurredAt;
        current.ClosureReason  = e.Reason;
        current.CurrentBalance = e.ClosingBalance;
        return current;
    }
}
