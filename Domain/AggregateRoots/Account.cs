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
    public string AccountNumber { get; private set; }
    public string AccountHolder { get; private set; }
    public Balance Balance { get; private set; }
    public AggregateStatus Status { get; private set; }
    public List<Transaction> Transactions { get; private set; }
    public DateTime OpenDate { get; private set; }
    public DateTime? CloseDate { get; private set; }

    // Snapshot support
    public long LastSnapshotVersion { get; set; }

    public Account() : base()
    {
        AccountNumber = string.Empty;
        AccountHolder = string.Empty;
        Balance = new Balance(new Money(0, "USD"));
        Status = AggregateStatus.Active;
        Transactions = new List<Transaction>();
        OpenDate = DateTime.UtcNow;
    }

    public Account(string id) : base(id)
    {
        AccountNumber = string.Empty;
        AccountHolder = string.Empty;
        Balance = new Balance(new Money(0, "USD"));
        Status = AggregateStatus.Active;
        Transactions = new List<Transaction>();
        OpenDate = DateTime.UtcNow;
    }

    // Create and open a new account
    public void CreateAccount(string accountNumber, string accountHolder, string currency, decimal initialBalance)
    {
        if (Version != 0)
            throw new DomainException("Account already created.", "ACCOUNT_ALREADY_CREATED");

        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new DomainException("Account number is required.", "INVALID_ACCOUNT_NUMBER");

        if (string.IsNullOrWhiteSpace(accountHolder))
            throw new DomainException("Account holder is required.", "INVALID_ACCOUNT_HOLDER");

        var initialMoney = new Money(initialBalance, currency);

        var @event = new AccountCreatedEvent(Id, accountNumber, accountHolder, currency, initialBalance)
        {
            UserId = "System"
        };

        RaiseEvent(@event);
    }

    // Deposit funds into the account
    public void Deposit(decimal amount, string reference)
    {
        if (Status != AggregateStatus.Active)
            throw new DomainException($"Cannot deposit into account with status {Status}.", "INVALID_ACCOUNT_STATUS");

        if (amount <= 0)
            throw new DomainException("Deposit amount must be greater than zero.", "INVALID_AMOUNT");

        var money = new Money(amount, Balance.CurrentAmount.Currency);
        var @event = new MoneyDepositedEvent(Id, amount, reference, Version + 1);

        RaiseEvent(@event);
    }

    // Withdraw funds from the account
    public void Withdraw(decimal amount, string reference)
    {
        if (Status != AggregateStatus.Active)
            throw new DomainException($"Cannot withdraw from account with status {Status}.", "INVALID_ACCOUNT_STATUS");

        if (amount <= 0)
            throw new DomainException("Withdrawal amount must be greater than zero.", "INVALID_AMOUNT");

        var money = new Money(amount, Balance.CurrentAmount.Currency);

        if (Balance.AvailableAmount.IsLessThan(money))
            throw new DomainException($"Insufficient balance. Available: {Balance.AvailableAmount}", "INSUFFICIENT_FUNDS");

        var @event = new MoneyWithdrawnEvent(Id, amount, reference, Version + 1);

        RaiseEvent(@event);
    }

    // Close the account
    public void CloseAccount(string reason)
    {
        if (Status == AggregateStatus.Closed)
            throw new DomainException("Account is already closed.", "ACCOUNT_ALREADY_CLOSED");

        var @event = new AccountClosedEvent(Id, reason, Balance.CurrentAmount.Amount, Version + 1);

        RaiseEvent(@event);
    }

    // Apply domain events to update state
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

    public override string ToString()
        => $"Account {{ Number={AccountNumber}, Holder={AccountHolder}, Balance={Balance.CurrentAmount}, Status={Status}, Version={Version} }}";
}
