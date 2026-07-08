#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Represents the lifecycle status of an account within the read model.
/// </summary>
public enum AccountReadModelStatus
{
    /// <summary>Account is open and accepting deposits and withdrawals.</summary>
    Active,

    /// <summary>Account has been permanently closed; no further transactions are permitted.</summary>
    Closed
}

/// <summary>
/// Immutable summary of a single credit or debit entry recorded against an account.
/// Appended to <see cref="AccountReadModel.Transactions"/> by <see cref="AccountProjector"/>.
/// </summary>
/// <param name="EventId">Identifier of the domain event that produced this transaction entry.</param>
/// <param name="Type">Direction of the movement: <c>"Deposit"</c> or <c>"Withdrawal"</c>.</param>
/// <param name="Amount">Absolute monetary amount of the transaction (always positive).</param>
/// <param name="Currency">ISO 4217 currency code inherited from the account (e.g. <c>USD</c>).</param>
/// <param name="Reference">Human-readable reference text supplied at the time of the transaction.</param>
/// <param name="ProcessedAt">UTC timestamp when the transaction was processed on the command side.</param>
public sealed record TransactionSummary(
    string EventId,
    string Type,
    decimal Amount,
    string Currency,
    string Reference,
    DateTime ProcessedAt);

/// <summary>
/// Eventually consistent materialized view of an <c>Account</c> aggregate.
/// Maintained by <see cref="AccountProjector"/> and stored in
/// <see cref="IReadModelStore{TReadModel}"/> keyed by aggregate identifier.
/// </summary>
/// <remarks>
/// This class is intentionally mutable so that the projector can apply incremental
/// updates without allocating a full replacement object on every event.
/// Consumers on the query side should treat the returned instance as read-only;
/// all writes must go through the command side.
/// </remarks>
public sealed class AccountReadModel
{
    // -----------------------------------------------------------------------
    // Identity — set once at creation, never mutated afterwards
    // -----------------------------------------------------------------------

    /// <summary>
    /// Aggregate identifier that also serves as the read model store key.
    /// Matches <c>AggregateRoot.Id</c> on the write side.
    /// </summary>
    public required string AccountId { get; init; }

    /// <summary>
    /// Human-readable account number assigned at creation (e.g. <c>ACC-0001</c>).
    /// </summary>
    public required string AccountNumber { get; init; }

    /// <summary>Full name of the account holder as supplied at account creation.</summary>
    public required string AccountHolder { get; init; }

    /// <summary>ISO 4217 currency code for all monetary values in this read model.</summary>
    public required string Currency { get; init; }

    /// <summary>UTC timestamp when the account was first created on the write side.</summary>
    public required DateTime OpenedAt { get; init; }

    // -----------------------------------------------------------------------
    // Mutable state — updated with every projected event
    // -----------------------------------------------------------------------

    /// <summary>
    /// Current account balance, recalculated after each deposit, withdrawal, and
    /// balance-adjustment event is projected.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Lifecycle status of the account.</summary>
    public AccountReadModelStatus Status { get; set; } = AccountReadModelStatus.Active;

    /// <summary>UTC timestamp when the account was closed, or <see langword="null"/> if still open.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Reason supplied when the account was closed, or <see langword="null"/> if still open.</summary>
    public string? ClosureReason { get; set; }

    /// <summary>
    /// Ordered list of transaction summaries applied to this account since creation.
    /// The list grows by one entry for every <c>MoneyDeposited</c> and <c>MoneyWithdrawn</c> event.
    /// </summary>
    public List<TransactionSummary> Transactions { get; set; } = [];

    /// <summary>
    /// Running total of all deposited amounts, including the initial balance set at account creation.
    /// </summary>
    public decimal TotalDeposited { get; set; }

    /// <summary>Running total of all withdrawn amounts since account creation.</summary>
    public decimal TotalWithdrawn { get; set; }

    /// <summary>
    /// Aggregate version of the most recent domain event projected into this read model.
    /// Used by <see cref="AccountProjector"/> to discard stale or duplicate deliveries.
    /// </summary>
    public long ProjectedVersion { get; set; }

    /// <summary>UTC timestamp of the last successful projection update.</summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    // -----------------------------------------------------------------------
    // Computed properties — derived from the above, never stored separately
    // -----------------------------------------------------------------------

    /// <summary>Total number of individual transactions recorded since account creation.</summary>
    public int TransactionCount => Transactions.Count;

    /// <summary>Convenience alias for <see cref="AccountId"/>.</summary>
    public string Id => AccountId;

    /// <summary>Convenience alias for <see cref="AccountHolder"/>.</summary>
    public string AccountHolderName => AccountHolder;

    /// <summary>Convenience alias for <see cref="CurrentBalance"/>.</summary>
    public decimal Balance => CurrentBalance;

    /// <summary>
    /// Net flow of funds through the account: <see cref="TotalDeposited"/> minus
    /// <see cref="TotalWithdrawn"/>. A positive value indicates net inflows.
    /// </summary>
    public decimal NetFlow => TotalDeposited - TotalWithdrawn;

    /// <summary>
    /// Returns <see langword="true"/> when the account is open and its balance is
    /// above zero — a quick eligibility check used by query services.
    /// </summary>
    public bool IsEligibleForWithdrawal =>
        Status == AccountReadModelStatus.Active && CurrentBalance > 0m;
}
