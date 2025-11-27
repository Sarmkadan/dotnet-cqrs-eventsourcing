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
    AccountCreated = 1,
    AccountOpened = 2,
    AccountClosed = 3,
    MoneyDeposited = 4,
    MoneyWithdrawn = 5,
    BalanceUpdated = 6,

    // Projection events
    ProjectionInitialized = 10,
    ProjectionUpdated = 11,
    ProjectionRebuilt = 12,

    // Snapshot events
    SnapshotTaken = 20,
    SnapshotRestored = 21,

    // System events
    EventReplayed = 30,
    ReplayStarted = 31,
    ReplayCompleted = 32,
}

/// <summary>
/// Represents the lifecycle status of an aggregate.
/// </summary>
public enum AggregateStatus
{
    Active = 0,
    Suspended = 1,
    Closed = 2,
    Archived = 3,
}

/// <summary>
/// Represents the type of transaction or money movement.
/// </summary>
public enum TransactionType
{
    Deposit = 0,
    Withdrawal = 1,
    Transfer = 2,
    Interest = 3,
    Fee = 4,
}
