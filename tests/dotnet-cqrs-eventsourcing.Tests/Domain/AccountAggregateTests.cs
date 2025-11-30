// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Domain;

using DotNetCqrsEventSourcing.Domain.AggregateRoots;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Shared.Enums;
using DotNetCqrsEventSourcing.Shared.Exceptions;
using FluentAssertions;
using Xunit;

public class AccountAggregateTests
{
    private static Account CreateFreshAccount(string accountNumber = "ACC-001",
        string holder = "Jane Doe", string currency = "USD", decimal initialBalance = 500m)
    {
        var account = new Account();
        account.CreateAccount(accountNumber, holder, currency, initialBalance);
        account.ClearUncommittedEvents();
        return account;
    }

    [Fact]
    public void CreateAccount_ValidParameters_RaisesAccountCreatedEvent()
    {
        var account = new Account();

        account.CreateAccount("ACC-001", "John Smith", "USD", 1000m);

        var events = account.GetUncommittedEvents();
        events.Should().HaveCount(1);
        events[0].Should().BeOfType<AccountCreatedEvent>();

        var created = (AccountCreatedEvent)events[0];
        created.AccountNumber.Should().Be("ACC-001");
        created.AccountHolder.Should().Be("John Smith");
        created.Currency.Should().Be("USD");
        created.InitialBalance.Should().Be(1000m);
    }

    [Fact]
    public void CreateAccount_ValidParameters_SetsAccountPropertiesCorrectly()
    {
        var account = new Account();

        account.CreateAccount("ACC-002", "Alice Brown", "EUR", 250m);

        account.AccountNumber.Should().Be("ACC-002");
        account.AccountHolder.Should().Be("Alice Brown");
        account.Balance.CurrentAmount.Amount.Should().Be(250m);
        account.Status.Should().Be(AggregateStatus.Active);
        account.Version.Should().Be(1);
    }

    [Fact]
    public void CreateAccount_AlreadyCreated_ThrowsDomainException()
    {
        var account = CreateFreshAccount();

        var act = () => account.CreateAccount("ACC-999", "Other Person", "USD", 0m);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("ACCOUNT_ALREADY_CREATED");
    }

    [Fact]
    public void CreateAccount_EmptyAccountNumber_ThrowsDomainException()
    {
        var account = new Account();

        var act = () => account.CreateAccount("", "John Smith", "USD", 0m);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_ACCOUNT_NUMBER");
    }

    [Fact]
    public void CreateAccount_EmptyAccountHolder_ThrowsDomainException()
    {
        var account = new Account();

        var act = () => account.CreateAccount("ACC-001", "   ", "USD", 0m);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_ACCOUNT_HOLDER");
    }

    [Fact]
    public void Deposit_PositiveAmount_IncreasesBalanceAndRecordsTransaction()
    {
        var account = CreateFreshAccount(initialBalance: 100m);

        account.Deposit(200m, "REF-001");

        account.Balance.CurrentAmount.Amount.Should().Be(300m);
        account.Transactions.Should().HaveCount(1);
        account.Transactions[0].Type.Should().Be(TransactionType.Deposit);
    }

    [Fact]
    public void Deposit_ZeroAmount_ThrowsDomainException()
    {
        var account = CreateFreshAccount();

        var act = () => account.Deposit(0m, "REF-X");

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public void Deposit_NegativeAmount_ThrowsDomainException()
    {
        var account = CreateFreshAccount();

        var act = () => account.Deposit(-50m, "REF-X");

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public void Deposit_ClosedAccount_ThrowsDomainException()
    {
        var account = CreateFreshAccount();
        account.CloseAccount("Test closure");
        account.ClearUncommittedEvents();

        var act = () => account.Deposit(100m, "REF-X");

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_ACCOUNT_STATUS");
    }

    [Fact]
    public void Withdraw_SufficientFunds_DecreasesBalanceAndRecordsTransaction()
    {
        var account = CreateFreshAccount(initialBalance: 400m);

        account.Withdraw(150m, "REF-002");

        account.Balance.CurrentAmount.Amount.Should().Be(250m);
        account.Transactions.Should().HaveCount(1);
        account.Transactions[0].Type.Should().Be(TransactionType.Withdrawal);
    }

    [Fact]
    public void Withdraw_InsufficientFunds_ThrowsDomainException()
    {
        var account = CreateFreshAccount(initialBalance: 100m);

        var act = () => account.Withdraw(500m, "REF-003");

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INSUFFICIENT_FUNDS");
    }

    [Fact]
    public void Withdraw_ZeroAmount_ThrowsDomainException()
    {
        var account = CreateFreshAccount(initialBalance: 100m);

        var act = () => account.Withdraw(0m, "REF-X");

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_AMOUNT");
    }

    [Fact]
    public void CloseAccount_ActiveAccount_SetsStatusToClosedAndRaisesEvent()
    {
        var account = CreateFreshAccount();

        account.CloseAccount("Customer request");

        account.Status.Should().Be(AggregateStatus.Closed);
        account.CloseDate.Should().NotBeNull();

        var events = account.GetUncommittedEvents();
        events.Should().ContainSingle(e => e is AccountClosedEvent);
    }

    [Fact]
    public void CloseAccount_AlreadyClosedAccount_ThrowsDomainException()
    {
        var account = CreateFreshAccount();
        account.CloseAccount("First close");
        account.ClearUncommittedEvents();

        var act = () => account.CloseAccount("Second close");

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("ACCOUNT_ALREADY_CLOSED");
    }

    [Fact]
    public void LoadFromHistory_RebuildsAccountStateFromEvents()
    {
        var accountId = Guid.NewGuid().ToString();

        var history = new List<DomainEvent>
        {
            new AccountCreatedEvent(accountId, "ACC-100", "Bob Builder", "USD", 0m)
            {
                AggregateVersion = 1
            },
            new MoneyDepositedEvent(accountId, 300m, "INIT", 2)
            {
                AggregateVersion = 2
            },
            new MoneyWithdrawnEvent(accountId, 100m, "ATM", 3)
            {
                AggregateVersion = 3
            }
        };

        var account = new Account(accountId);
        account.LoadFromHistory(history);

        account.AccountNumber.Should().Be("ACC-100");
        account.AccountHolder.Should().Be("Bob Builder");
        account.Balance.CurrentAmount.Amount.Should().Be(200m);
        account.Version.Should().Be(3);
        account.Transactions.Should().HaveCount(2);
        account.GetUncommittedEvents().Should().BeEmpty();
    }

    [Fact]
    public void ClearUncommittedEvents_AfterRaisingEvents_EmptiesQueue()
    {
        var account = new Account();
        account.CreateAccount("ACC-CLEAR", "Test User", "USD", 100m);

        account.GetUncommittedEvents().Should().HaveCount(1);
        account.ClearUncommittedEvents();
        account.GetUncommittedEvents().Should().BeEmpty();
    }
}
