#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Application;

using System;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Data.Repositories;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public sealed class AccountServiceTests
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
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(CreateAccountAsync_ValidParameters_ReturnsSuccessWithAccount));

        try
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
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(CreateAccountAsync_ValidParameters_ReturnsSuccessWithAccount));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(CreateAccountAsync_ValidParameters_ReturnsSuccessWithAccount));
        }
    }

    [Fact]
    public async Task CreateAccountAsync_RepositorySaveFails_ReturnsFailure()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(CreateAccountAsync_RepositorySaveFails_ReturnsFailure));

        try
        {
            _repositoryMock
                .Setup(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure("SAVE_ERROR", "Database unavailable"));

            var result = await _sut.CreateAccountAsync("ACC-501", "Test User", "USD", 0m);

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("SAVE_ERROR");
        }
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(CreateAccountAsync_RepositorySaveFails_ReturnsFailure));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(CreateAccountAsync_RepositorySaveFails_ReturnsFailure));
        }
    }

    [Fact]
    public async Task CreateAccountAsync_InvalidDomainOperation_ReturnsFailureWithCode()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(CreateAccountAsync_InvalidDomainOperation_ReturnsFailureWithCode));

        try
        {
            var result = await _sut.CreateAccountAsync("", "Test User", "USD", 0m);

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("CREATE_ACCOUNT_FAILED");
            _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(CreateAccountAsync_InvalidDomainOperation_ReturnsFailureWithCode));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(CreateAccountAsync_InvalidDomainOperation_ReturnsFailureWithCode));
        }
    }

    [Fact]
    public async Task DepositAsync_AccountNotFound_ReturnsFailure()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(DepositAsync_AccountNotFound_ReturnsFailure));

        try
        {
            var missingId = Guid.NewGuid().ToString();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Account>.Failure("NOT_FOUND", "Account not found"));

            var result = await _sut.DepositAsync(missingId, 200m, "REF-X");

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("NOT_FOUND");
        }
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(DepositAsync_AccountNotFound_ReturnsFailure));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(DepositAsync_AccountNotFound_ReturnsFailure));
        }
    }

    [Fact]
    public async Task DepositAsync_ValidAccount_SavesAndPublishesEvents()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(DepositAsync_ValidAccount_SavesAndPublishesEvents));

        try
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
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(DepositAsync_ValidAccount_SavesAndPublishesEvents));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(DepositAsync_ValidAccount_SavesAndPublishesEvents));
        }
    }

    [Fact]
    public async Task WithdrawAsync_InsufficientFunds_ReturnsFailure()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(WithdrawAsync_InsufficientFunds_ReturnsFailure));

        try
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
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(WithdrawAsync_InsufficientFunds_ReturnsFailure));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(WithdrawAsync_InsufficientFunds_ReturnsFailure));
        }
    }

    [Fact]
    public async Task CloseAccountAsync_ValidAccount_SucceedsAndPublishesClosedEvent()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(CloseAccountAsync_ValidAccount_SucceedsAndPublishesClosedEvent));

        try
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
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(CloseAccountAsync_ValidAccount_SucceedsAndPublishesClosedEvent));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(CloseAccountAsync_ValidAccount_SucceedsAndPublishesClosedEvent));
        }
    }

    [Fact]
    public async Task GetTransactionCountAsync_AfterDeposit_ReturnsCorrectCount()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(GetTransactionCountAsync_AfterDeposit_ReturnsCorrectCount));

        try
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
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(GetTransactionCountAsync_AfterDeposit_ReturnsCorrectCount));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(GetTransactionCountAsync_AfterDeposit_ReturnsCorrectCount));
        }
    }

    [Fact]
    public async Task CreateAccountAsync_InvalidCurrency_ReturnsFailure()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(CreateAccountAsync_InvalidCurrency_ReturnsFailure));

        try
        {
            var result = await _sut.CreateAccountAsync("ACC-001", "User", "INVALID", 100m);

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("CREATE_ACCOUNT_FAILED");
        }
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(CreateAccountAsync_InvalidCurrency_ReturnsFailure));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(CreateAccountAsync_InvalidCurrency_ReturnsFailure));
        }
    }

    [Fact]
    public async Task GetAccountAsync_RepositoryThrowsException_ReturnsFailure()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(GetAccountAsync_RepositoryThrowsException_ReturnsFailure));

        try
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            var result = await _sut.GetAccountAsync("ACC-123");

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("GET_ACCOUNT_FAILED");
        }
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(GetAccountAsync_RepositoryThrowsException_ReturnsFailure));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(GetAccountAsync_RepositoryThrowsException_ReturnsFailure));
        }
    }

    [Fact]
    public async Task WithdrawAsync_RepositoryThrowsException_ReturnsFailure()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(WithdrawAsync_RepositoryThrowsException_ReturnsFailure));

        try
        {
            var account = new Account();
            account.CreateAccount("ACC-100", "User", "USD", 100m);
            account.ClearUncommittedEvents();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Account>.Success(account));

            _repositoryMock
                .Setup(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB Error"));

            var result = await _sut.WithdrawAsync(account.Id, 50m, "REF");

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("WITHDRAWAL_FAILED");
        }
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(WithdrawAsync_RepositoryThrowsException_ReturnsFailure));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(WithdrawAsync_RepositoryThrowsException_ReturnsFailure));
        }
    }

    [Fact]
    public async Task DepositAsync_RepositoryThrowsException_ReturnsFailure()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(DepositAsync_RepositoryThrowsException_ReturnsFailure));

        try
        {
            var account = new Account();
            account.CreateAccount("ACC-100", "User", "USD", 100m);
            account.ClearUncommittedEvents();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Account>.Success(account));

            _repositoryMock
                .Setup(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB Error"));

            var result = await _sut.DepositAsync(account.Id, 50m, "REF");

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("DEPOSIT_FAILED");
        }
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(DepositAsync_RepositoryThrowsException_ReturnsFailure));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(DepositAsync_RepositoryThrowsException_ReturnsFailure));
        }
    }

    [Fact]
    public async Task CloseAccountAsync_RepositoryThrowsException_ReturnsFailure()
    {
        _loggerMock.Object.LogInformation("Starting {TestMethod}", nameof(CloseAccountAsync_RepositoryThrowsException_ReturnsFailure));

        try
        {
            var account = new Account();
            account.CreateAccount("ACC-100", "User", "USD", 100m);
            account.ClearUncommittedEvents();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Account>.Success(account));

            _repositoryMock
                .Setup(r => r.SaveAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB Error"));

            var result = await _sut.CloseAccountAsync(account.Id, "Reason");

            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be("CLOSE_ACCOUNT_FAILED");
        }
        catch (Exception ex)
        {
            _loggerMock.Object.LogError(ex, "Error in {TestMethod}", nameof(CloseAccountAsync_RepositoryThrowsException_ReturnsFailure));
            throw;
        }
        finally
        {
            _loggerMock.Object.LogInformation("Finished {TestMethod}", nameof(CloseAccountAsync_RepositoryThrowsException_ReturnsFailure));
        }
    }
}
