// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Application;

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Data.Repositories;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class AccountServiceTests
{
    private readonly Mock<IRepository<Account>> _repositoryMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Account>>();
        _eventBusMock = new Mock<IEventBus>();
        _loggerMock = new Mock<ILogger<AccountService>>();
        _sut = new AccountService(_repositoryMock.Object, _eventBusMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAccountAsync_ValidParameters_ReturnsSuccessWithAccount()
    {
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _eventBusMock
            .Setup(b => b.PublishEventsAsync(It.IsAny<List<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.CreateAccountAsync("ACC-500", "Maria Garcia", "USD", 1000m);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccountNumber.Should().Be("ACC-500");
        result.Data.AccountHolder.Should().Be("Maria Garcia");
    }

    [Fact]
    public async Task CreateAccountAsync_RepositorySaveFails_ReturnsFailure()
    {
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("SAVE_ERROR", "Database unavailable"));

        var result = await _sut.CreateAccountAsync("ACC-501", "Test User", "USD", 0m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("SAVE_ERROR");
    }

    [Fact]
    public async Task CreateAccountAsync_InvalidDomainOperation_ReturnsFailureWithCode()
    {
        var result = await _sut.CreateAccountAsync("", "Test User", "USD", 0m);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CREATE_ACCOUNT_FAILED");
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_AccountNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid().ToString();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Account>.Failure("NOT_FOUND", "Account not found"));

        var result = await _sut.DepositAsync(missingId, 200m, "REF-X");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task DepositAsync_ValidAccount_SavesAndPublishesEvents()
    {
        var account = new Account();
        account.CreateAccount("ACC-600", "Sam Lee", "USD", 500m);
        account.ClearUncommittedEvents();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Account>.Success(account));

        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _eventBusMock
            .Setup(b => b.PublishEventsAsync(It.IsAny<List<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.DepositAsync(account.Id, 300m, "REF-DEP");

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(b => b.PublishEventsAsync(It.IsAny<List<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_InsufficientFunds_ReturnsFailure()
    {
        var account = new Account();
        account.CreateAccount("ACC-700", "Paul Kim", "USD", 100m);
        account.ClearUncommittedEvents();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Account>.Success(account));

        var result = await _sut.WithdrawAsync(account.Id, 9999m, "REF-OVER");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("WITHDRAWAL_FAILED");
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseAccountAsync_ValidAccount_SucceedsAndPublishesClosedEvent()
    {
        var account = new Account();
        account.CreateAccount("ACC-800", "Lisa Monroe", "USD", 250m);
        account.ClearUncommittedEvents();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Account>.Success(account));

        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _eventBusMock
            .Setup(b => b.PublishEventsAsync(It.IsAny<List<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.CloseAccountAsync(account.Id, "Customer closed account");

        result.IsSuccess.Should().BeTrue();

        _eventBusMock.Verify(b => b.PublishEventsAsync(
            It.Is<List<DomainEvent>>(events => events.Any(e => e is AccountClosedEvent)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTransactionCountAsync_AfterDeposit_ReturnsCorrectCount()
    {
        var account = new Account();
        account.CreateAccount("ACC-900", "Nina Patel", "USD", 200m);
        account.Deposit(100m, "REF-1");
        account.Deposit(50m, "REF-2");
        account.ClearUncommittedEvents();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Account>.Success(account));

        var result = await _sut.GetTransactionCountAsync(account.Id);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(2);
    }
}
