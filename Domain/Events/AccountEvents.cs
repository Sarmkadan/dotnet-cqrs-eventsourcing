#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Event raised when an account is created in the system.
/// </summary>
[EventName("AccountCreated")]
public class AccountCreatedEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the account number.
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the account holder name.
    /// </summary>
    public string AccountHolder { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the currency code (e.g., USD, EUR).
    /// </summary>
    public string Currency { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the initial account balance.
    /// </summary>
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

    public AccountCreatedEvent(string aggregateId, string accountNumber, string accountHolder,
        string currency, decimal initialBalance, DateTime occurredAt)
        : this(aggregateId, accountNumber, accountHolder, currency, initialBalance)
    {
        OccurredAt = occurredAt;
    }

    /// <summary>
    /// Convenience alias for <see cref="AccountHolder"/>.
    /// Excluded from serialization so stored event payloads remain unchanged.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AccountHolderName => AccountHolder;

    public override string GetEventType() => "AccountCreated";

    public override string ToString() => $"AccountCreatedEvent {{ AccountNumber = {AccountNumber}, AccountHolder = {AccountHolder}, Currency = {Currency}, InitialBalance = {InitialBalance} }}";
}

/// <summary>
/// Event raised when money is deposited into an account.
/// </summary>
[EventName("MoneyDeposited")]
public class MoneyDepositedEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the deposit amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the transaction reference.
    /// </summary>
    public string Reference { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the date and time when the deposit was processed.
    /// </summary>
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
[EventName("MoneyWithdrawn")]
public class MoneyWithdrawnEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the withdrawal amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the transaction reference.
    /// </summary>
    public string Reference { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the date and time when the withdrawal was processed.
    /// </summary>
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
[EventName("BalanceUpdated")]
public class BalanceUpdatedEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the previous account balance.
    /// </summary>
    public decimal PreviousBalance { get; set; }
    /// <summary>
    /// Gets or sets the new account balance.
    /// </summary>
    public decimal NewBalance { get; set; }
    /// <summary>
    /// Gets or sets the reason for the balance update.
    /// </summary>
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
[EventName("AccountClosed")]
public class AccountClosedEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the reason for closing the account.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the account balance at the time of closure.
    /// </summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountClosedEvent"/> class.
    /// </summary>
    public AccountClosedEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountClosedEvent"/> class with the specified parameters.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="reason">The reason for closing the account.</param>
    /// <param name="closingBalance">The account balance at the time of closure.</param>
    /// <param name="version">The version of the event.</param>
    public AccountClosedEvent(string aggregateId, string reason, decimal closingBalance, long version)
        : base(aggregateId, "Account", version)
    {
        Reason = reason;
        ClosingBalance = closingBalance;
    }

    public override string GetEventType() => "AccountClosed";
}
