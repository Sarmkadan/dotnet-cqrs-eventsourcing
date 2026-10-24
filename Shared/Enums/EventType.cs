#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Enums;

/// <summary>
/// Defines all domain event types used in the CQRS framework.
/// </summary>
public enum EventType
{
    // Account events

    /// <summary>
    /// An account was created.
    /// </summary>
    AccountCreated = 1,

    /// <summary>
    /// An account was opened for use.
    /// </summary>
    AccountOpened = 2,

    /// <summary>
    /// An account was closed.
    /// </summary>
    AccountClosed = 3,

    /// <summary>
    /// Money was deposited into an account.
    /// </summary>
    MoneyDeposited = 4,

    /// <summary>
    /// Money was withdrawn from an account.
    /// </summary>
    MoneyWithdrawn = 5,

    /// <summary>
    /// An account balance was updated.
    /// </summary>
    BalanceUpdated = 6,

    // Projection events

    /// <summary>
    /// A projection was initialized.
    /// </summary>
    ProjectionInitialized = 10,

    /// <summary>
    /// A projection was updated.
    /// </summary>
    ProjectionUpdated = 11,

    /// <summary>
    /// A projection was rebuilt from the event stream.
    /// </summary>
    ProjectionRebuilt = 12,

    // Snapshot events

    /// <summary>
    /// A snapshot of an aggregate was taken.
    /// </summary>
    SnapshotTaken = 20,

    /// <summary>
    /// An aggregate was restored from a snapshot.
    /// </summary>
    SnapshotRestored = 21,

    // System events

    /// <summary>
    /// An event was replayed.
    /// </summary>
    EventReplayed = 30,

    /// <summary>
    /// An event replay started.
    /// </summary>
    ReplayStarted = 31,

    /// <summary>
    /// An event replay completed.
    /// </summary>
    ReplayCompleted = 32,
}

/// <summary>
/// Represents the lifecycle status of an aggregate.
/// </summary>
public enum AggregateStatus
{
    /// <summary>
    /// The aggregate is active and accepts changes.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The aggregate is temporarily suspended.
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// The aggregate has been closed.
    /// </summary>
    Closed = 2,

    /// <summary>
    /// The aggregate has been archived.
    /// </summary>
    Archived = 3,
}

/// <summary>
/// Represents the type of transaction or money movement.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Money was added to an account.
    /// </summary>
    Deposit = 0,

    /// <summary>
    /// Money was removed from an account.
    /// </summary>
    Withdrawal = 1,

    /// <summary>
    /// Money was moved between accounts.
    /// </summary>
    Transfer = 2,

    /// <summary>
    /// Interest was credited to an account.
    /// </summary>
    Interest = 3,

    /// <summary>
    /// A fee was charged to an account.
    /// </summary>
    Fee = 4,
}
