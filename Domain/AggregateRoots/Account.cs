#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.AggregateRoots;

using Events;
using Shared.Enums;
using Shared.Exceptions;
using ValueObjects;

/// <summary>
/// Account aggregate root - manages the complete lifecycle of a bank account.
/// </summary>
public class Account : AggregateRoot
{
    /// <summary>
    /// Gets the unique account number.
    /// </summary>
    public string AccountNumber { get; private set; }

    /// <summary>
    /// Gets the name of the account holder.
    /// </summary>
    public string AccountHolder { get; private set; }

    /// <summary>
    /// Gets the current balance of the account.
    /// </summary>
    public Balance Balance { get; private set; }

    /// <summary>
    /// Gets the current lifecycle status of the account.
    /// </summary>
    public AggregateStatus Status { get; private set; }

    /// <summary>
    /// Gets the list of transactions recorded against the account.
    /// </summary>
    public List<Transaction> Transactions { get; private set; }

    /// <summary>
    /// Gets the date and time when the account was opened.
    /// </summary>
    public DateTime OpenDate { get; private set; }

    /// <summary>
    /// Gets the date and time when the account was closed, or <see langword="null"/> if it is still open.
    /// </summary>
    public DateTime? CloseDate { get; private set; }

    // Snapshot support
    /// <summary>
    /// Gets or sets the aggregate version captured by the most recent snapshot.
    /// </summary>
    public long LastSnapshotVersion { get; set; }

    /// <summary>
    /// Convenience alias for <see cref="AccountHolder"/>.
    /// Excluded from serialization so snapshot payloads remain unchanged.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string AccountHolderName => AccountHolder;

    /// <summary>
    /// Indicates whether the account has been closed.
    /// Excluded from serialization so snapshot payloads remain unchanged.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsClosed => Status == AggregateStatus.Closed;

    /// <summary>
    /// Replays the given event history onto this account and returns the account,
    /// enabling fluent state reconstruction. Equivalent to <see cref="AggregateRoot.LoadFromHistory"/>.
    /// </summary>
    public Account ReplayEvents(IEnumerable<DomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        LoadFromHistory(events);
        return this;
    }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="Account"/> class for event replay or persistence.
    /// </summary>
    public Account() : base()
    {
        AccountNumber = string.Empty;
        AccountHolder = string.Empty;
        Balance = new Balance(new Money(0, "USD"));
        Status = AggregateStatus.Active;
        Transactions = new List<Transaction>();
        OpenDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Account"/> class with the specified aggregate identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier of the account.</param>
    public Account(string id) : base(id)
    {
        ArgumentNullException.ThrowIfNull(id);
        AccountNumber = string.Empty;
        AccountHolder = string.Empty;
        Balance = new Balance(new Money(0, "USD"));
        Status = AggregateStatus.Active;
        Transactions = new List<Transaction>();
        OpenDate = DateTime.UtcNow;
    }

    // Create and open a new account
    /// <summary>
    /// Creates and opens the account, raising an <see cref="AccountCreatedEvent"/>.
    /// </summary>
    /// <param name="accountNumber">The unique account number.</param>
    /// <param name="accountHolder">The name of the account holder.</param>
    /// <param name="currency">The ISO currency code of the account.</param>
    /// <param name="initialBalance">The opening balance of the account.</param>
    /// <exception cref="DomainException">Thrown when the account already exists or the account number or holder is missing.</exception>
    public void CreateAccount(string accountNumber, string accountHolder, string currency, decimal initialBalance)
    {
        if (Version != 0)
        {
            throw new DomainException("Account already created.", "ACCOUNT_ALREADY_CREATED");
        }

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new DomainException("Account number is required.", "INVALID_ACCOUNT_NUMBER");
        }

        if (string.IsNullOrWhiteSpace(accountHolder))
        {
            throw new DomainException("Account holder is required.", "INVALID_ACCOUNT_HOLDER");
        }

        var initialMoney = new Money(initialBalance, currency);

        var @event = new AccountCreatedEvent(Id, accountNumber, accountHolder, currency, initialBalance)
        {
            UserId = "System"
        };

        RaiseEvent(@event);
    }

    // Deposit funds into the account
    /// <summary>
    /// Deposits funds into the account, raising a <see cref="MoneyDepositedEvent"/>.
    /// </summary>
    /// <param name="amount">The amount to deposit. Must be greater than zero.</param>
    /// <param name="reference">The reference describing the deposit.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reference"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reference"/> is empty.</exception>
    /// <exception cref="DomainException">Thrown when the account is not active or the amount is not positive.</exception>
    public void Deposit(decimal amount, string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentException.ThrowIfNullOrEmpty(reference);
        if (Status != AggregateStatus.Active)
        {
            throw new DomainException($"Cannot deposit into account with status {Status}.", "INVALID_ACCOUNT_STATUS");
        }

        if (amount <= 0)
        {
            throw new DomainException("Deposit amount must be greater than zero.", "INVALID_AMOUNT");
        }

        var money = new Money(amount, Balance.CurrentAmount.Currency);
        var @event = new MoneyDepositedEvent(Id, amount, reference, Version + 1);

        RaiseEvent(@event);
    }

    // Withdraw funds from the account
    /// <summary>
    /// Withdraws funds from the account, raising a <see cref="MoneyWithdrawnEvent"/>.
    /// </summary>
    /// <param name="amount">The amount to withdraw. Must be greater than zero.</param>
    /// <param name="reference">The reference describing the withdrawal.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reference"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reference"/> is empty.</exception>
    /// <exception cref="DomainException">Thrown when the account is not active, the amount is not positive, or funds are insufficient.</exception>
    public void Withdraw(decimal amount, string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentException.ThrowIfNullOrEmpty(reference);
        if (Status != AggregateStatus.Active)
        {
            throw new DomainException($"Cannot withdraw from account with status {Status}.", "INVALID_ACCOUNT_STATUS");
        }

        if (amount <= 0)
        {
            throw new DomainException("Withdrawal amount must be greater than zero.", "INVALID_AMOUNT");
        }

        var money = new Money(amount, Balance.CurrentAmount.Currency);

        if (Balance.AvailableAmount.IsLessThan(money))
        {
            throw new DomainException($"Insufficient balance. Available: {Balance.AvailableAmount}", "INSUFFICIENT_FUNDS");
        }

        var @event = new MoneyWithdrawnEvent(Id, amount, reference, Version + 1);

        RaiseEvent(@event);
    }

    // Close the account
    /// <summary>
    /// Closes the account, raising an <see cref="AccountClosedEvent"/>.
    /// </summary>
    /// <param name="reason">The reason for closing the account.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reason"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is empty.</exception>
    /// <exception cref="DomainException">Thrown when the account is already closed.</exception>
    public void CloseAccount(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        ArgumentException.ThrowIfNullOrEmpty(reason);
        if (Status == AggregateStatus.Closed)
        {
            throw new DomainException("Account is already closed.", "ACCOUNT_ALREADY_CLOSED");
        }

        var @event = new AccountClosedEvent(Id, reason, Balance.CurrentAmount.Amount, Version + 1);

        RaiseEvent(@event);
    }

    // Apply domain events to update state
    /// <summary>
    /// Applies a domain event to the account state by dispatching it to the matching handler.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    /// <param name="isFromHistory">
    /// <see langword="true"/> if the event is being replayed from history; otherwise, <see langword="false"/>.
    /// </param>
    /// <exception cref="DomainException">Thrown when the event type is not recognized.</exception>
    protected override void ApplyEvent(DomainEvent @event, bool isFromHistory)
    {
        switch (@event)
        {
            case AccountCreatedEvent accountCreated:
                ApplyAccountCreated(accountCreated);
                break;

            case MoneyDepositedEvent moneyDeposited:
                ApplyMoneyDeposited(moneyDeposited);
                break;

            case MoneyWithdrawnEvent moneyWithdrawn:
                ApplyMoneyWithdrawn(moneyWithdrawn);
                break;

            case AccountClosedEvent accountClosed:
                ApplyAccountClosed(accountClosed);
                break;

            case BalanceUpdatedEvent balanceUpdated:
                ApplyBalanceUpdated(balanceUpdated);
                break;

            default:
                throw new DomainException($"Unknown event type: {@event.GetType().Name}", "UNKNOWN_EVENT_TYPE");
        }
    }

    private void ApplyAccountCreated(AccountCreatedEvent @event)
    {
        AccountNumber = @event.AccountNumber;
        AccountHolder = @event.AccountHolder;
        Balance = new Balance(new Money(@event.InitialBalance, @event.Currency));
        Status = AggregateStatus.Active;
        OpenDate = @event.OccurredAt;
    }

    private void ApplyMoneyDeposited(MoneyDepositedEvent @event)
    {
        var money = new Money(@event.Amount, Balance.CurrentAmount.Currency);
        Balance.AddFunds(money);

        var transaction = new Transaction(
            TransactionType.Deposit,
            money,
            @event.Reference,
            $"Deposit on {DateTime.UtcNow:yyyy-MM-dd}"
        );
        Transactions.Add(transaction);
    }

    private void ApplyMoneyWithdrawn(MoneyWithdrawnEvent @event)
    {
        var money = new Money(@event.Amount, Balance.CurrentAmount.Currency);
        Balance.RemoveFunds(money);

        var transaction = new Transaction(
            TransactionType.Withdrawal,
            money,
            @event.Reference,
            $"Withdrawal on {DateTime.UtcNow:yyyy-MM-dd}"
        );
        Transactions.Add(transaction);
    }

    private void ApplyBalanceUpdated(BalanceUpdatedEvent @event)
    {
        // Balance is already updated by other events; this is just a record
    }

    private void ApplyAccountClosed(AccountClosedEvent @event)
    {
        Status = AggregateStatus.Closed;
        CloseDate = @event.OccurredAt;
    }

    /// <summary>
    /// Returns a string representation of the account including its number, holder, balance, status and version.
    /// </summary>
    /// <returns>A formatted string describing the account.</returns>
    public override string ToString()
        => $"Account {{ Number={AccountNumber}, Holder={AccountHolder}, Balance={Balance.CurrentAmount}, Status={Status}, Version={Version} }}";
}
