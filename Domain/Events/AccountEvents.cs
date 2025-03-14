// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Event raised when an account is created in the system.
/// </summary>
public class AccountCreatedEvent : DomainEvent
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }

    public AccountCreatedEvent() { }

    public AccountCreatedEvent(string aggregateId, string accountNumber, string accountHolder,
        string currency, decimal initialBalance)
        : base(aggregateId, "Account", 1)
    {
        AccountNumber = accountNumber;
        AccountHolder = accountHolder;
        Currency = currency;
        InitialBalance = initialBalance;
    }

    public override string GetEventType() => "AccountCreated";
}

/// <summary>
/// Event raised when money is deposited into an account.
/// </summary>
public class MoneyDepositedEvent : DomainEvent
{
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }

    public MoneyDepositedEvent() { }

    public MoneyDepositedEvent(string aggregateId, decimal amount, string reference, long version)
        : base(aggregateId, "Account", version)
    {
        Amount = amount;
        Reference = reference;
        ProcessedAt = DateTime.UtcNow;
    }

    public override string GetEventType() => "MoneyDeposited";
}

/// <summary>
/// Event raised when money is withdrawn from an account.
/// </summary>
public class MoneyWithdrawnEvent : DomainEvent
{
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }

    public MoneyWithdrawnEvent() { }

    public MoneyWithdrawnEvent(string aggregateId, decimal amount, string reference, long version)
        : base(aggregateId, "Account", version)
    {
        Amount = amount;
        Reference = reference;
        ProcessedAt = DateTime.UtcNow;
    }

    public override string GetEventType() => "MoneyWithdrawn";
}

/// <summary>
/// Event raised when account balance is updated.
/// </summary>
public class BalanceUpdatedEvent : DomainEvent
{
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public string Reason { get; set; } = string.Empty;

    public BalanceUpdatedEvent() { }

    public BalanceUpdatedEvent(string aggregateId, decimal previousBalance, decimal newBalance,
        string reason, long version)
        : base(aggregateId, "Account", version)
    {
        PreviousBalance = previousBalance;
        NewBalance = newBalance;
        Reason = reason;
    }

    public override string GetEventType() => "BalanceUpdated";
}

/// <summary>
/// Event raised when an account is closed.
/// </summary>
public class AccountClosedEvent : DomainEvent
{
    public string Reason { get; set; } = string.Empty;
    public decimal ClosingBalance { get; set; }

    public AccountClosedEvent() { }

    public AccountClosedEvent(string aggregateId, string reason, decimal closingBalance, long version)
        : base(aggregateId, "Account", version)
    {
        Reason = reason;
        ClosingBalance = closingBalance;
    }

    public override string GetEventType() => "AccountClosed";
}
