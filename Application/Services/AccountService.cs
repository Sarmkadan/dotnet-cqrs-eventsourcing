#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Data.Repositories;
using Domain.AggregateRoots;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Results;

/// <summary>
/// Account service implementation handling account operations and persistence.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IRepository<Account> _accountRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AccountService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="accountRepository">Repository used to load and persist account aggregates.</param>
    /// <param name="eventBus">Event bus used to publish domain events after a successful save.</param>
    /// <param name="logger">Logger used to record account operations and failures.</param>
    public AccountService(IRepository<Account> accountRepository, IEventBus eventBus, ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new account, persists it and publishes its uncommitted domain events.
    /// </summary>
    /// <param name="accountNumber">The unique account number.</param>
    /// <param name="accountHolder">The name of the account holder.</param>
    /// <param name="currency">The ISO currency code for the account.</param>
    /// <param name="initialBalance">The opening balance of the account.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the created account, or a failure if creation or persistence failed.</returns>
    public async Task<Result<Account>> CreateAccountAsync(string accountNumber, string accountHolder,
        string currency, decimal initialBalance, CancellationToken cancellationToken = default)
    {
        try
        {
            // Input validation is owned by the domain: Account.CreateAccount and the Money
            // value object throw DomainException for invalid arguments, which is translated
            // below into a CREATE_ACCOUNT_FAILED result instead of leaking exceptions.
            var account = new Account();
            account.CreateAccount(accountNumber, accountHolder, currency, initialBalance);

            var events = account.GetUncommittedEvents().Cast<Domain.Events.DomainEvent>().ToList();
            var saveResult = await _accountRepository.SaveAsync(account, cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result<Account>.Failure(saveResult.ErrorCode!, saveResult.ErrorMessage!);
            }

            try
            {
                await _eventBus.PublishEventsAsync(events, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Account {AccountNumber} was created but its events could not be published", account.AccountNumber);
            }

            _logger.LogInformation("Account created: {AccountNumber} for {AccountHolder}", accountNumber, accountHolder);
            return Result<Account>.Success(account);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return Result<Account>.Failure("CREATE_ACCOUNT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Loads the account with the specified identifier.
    /// </summary>
    /// <param name="accountId">The identifier of the account to load.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the account, or a failure if it could not be loaded.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is null or whitespace.</exception>
    public async Task<Result<Account>> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));
        }

        try
        {
            return await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {AccountId}", accountId);
            return Result<Account>.Failure("GET_ACCOUNT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Deposits funds into the specified account and publishes the resulting domain events.
    /// </summary>
    /// <param name="accountId">The identifier of the target account.</param>
    /// <param name="amount">The amount to deposit; must be greater than zero.</param>
    /// <param name="reference">A reference describing the deposit.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the deposit is saved, or a failure otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="reference"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is not greater than zero.</exception>
    public async Task<Result> DepositAsync(string accountId, decimal amount, string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException("Reference cannot be null or whitespace.", nameof(reference));
        }

        try
        {
            var accountResult = await GetAccountAsync(accountId, cancellationToken);
            if (!accountResult.IsSuccess)
            {
                return Result.Failure(accountResult.ErrorCode!, accountResult.ErrorMessage!);
            }

            var account = accountResult.Data!;
            account.Deposit(amount, reference);

            var events = account.GetUncommittedEvents().Cast<Domain.Events.DomainEvent>().ToList();
            var saveResult = await _accountRepository.SaveAsync(account, cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            try
            {
                await _eventBus.PublishEventsAsync(events, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Deposit was saved for account {AccountNumber} but its events could not be published", account.AccountNumber);
            }

            _logger.LogInformation("Deposit processed: {Amount} to account {AccountId}", amount, accountId);
            return Result.Success();
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit to account {AccountId}", accountId);
            return Result.Failure("DEPOSIT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Withdraws funds from the specified account and publishes the resulting domain events.
    /// </summary>
    /// <param name="accountId">The identifier of the source account.</param>
    /// <param name="amount">The amount to withdraw; must be greater than zero.</param>
    /// <param name="reference">A reference describing the withdrawal.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the withdrawal is saved, or a failure otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="reference"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is not greater than zero.</exception>
    public async Task<Result> WithdrawAsync(string accountId, decimal amount, string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException("Reference cannot be null or whitespace.", nameof(reference));
        }

        try
        {
            var accountResult = await GetAccountAsync(accountId, cancellationToken);
            if (!accountResult.IsSuccess)
            {
                return Result.Failure(accountResult.ErrorCode!, accountResult.ErrorMessage!);
            }

            var account = accountResult.Data!;
            account.Withdraw(amount, reference);

            var events = account.GetUncommittedEvents().Cast<Domain.Events.DomainEvent>().ToList();
            var saveResult = await _accountRepository.SaveAsync(account, cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            try
            {
                await _eventBus.PublishEventsAsync(events, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Withdrawal was saved for account {AccountNumber} but its events could not be published", account.AccountNumber);
            }

            _logger.LogInformation("Withdrawal processed: {Amount} from account {AccountId}", amount, accountId);
            return Result.Success();
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing withdrawal from account {AccountId}", accountId);
            return Result.Failure("WITHDRAWAL_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Closes the specified account with the given reason and publishes the resulting domain events.
    /// </summary>
    /// <param name="accountId">The identifier of the account to close.</param>
    /// <param name="reason">The reason for closing the account.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the account is closed and saved, or a failure otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="reason"/> is null or whitespace.</exception>
    public async Task<Result> CloseAccountAsync(string accountId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason cannot be null or whitespace.", nameof(reason));
        }

        try
        {
            var accountResult = await GetAccountAsync(accountId, cancellationToken);
            if (!accountResult.IsSuccess)
            {
                return Result.Failure(accountResult.ErrorCode!, accountResult.ErrorMessage!);
            }

            var account = accountResult.Data!;
            account.CloseAccount(reason);

            var events = account.GetUncommittedEvents().Cast<Domain.Events.DomainEvent>().ToList();
            var saveResult = await _accountRepository.SaveAsync(account, cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            try
            {
                await _eventBus.PublishEventsAsync(events, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Account {AccountNumber} was closed but its events could not be published", account.AccountNumber);
            }

            _logger.LogInformation("Account closed: {AccountId} - Reason: {Reason}", accountId, reason);
            return Result.Success();
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing account {AccountId}", accountId);
            return Result.Failure("CLOSE_ACCOUNT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Retrieves all accounts from the repository.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the list of accounts, or a failure if retrieval failed.</returns>
    public async Task<Result<List<Account>>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _accountRepository.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all accounts");
            return Result<List<Account>>.Failure("GET_ACCOUNTS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets the number of monetary transactions recorded on the specified account.
    /// </summary>
    /// <param name="accountId">The identifier of the account.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the transaction count, or a failure if the account could not be loaded.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is null or whitespace.</exception>
    public async Task<Result<int>> GetTransactionCountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));
        }

        try
        {
            var accountResult = await GetAccountAsync(accountId, cancellationToken);
            if (!accountResult.IsSuccess)
            {
                return Result<int>.Failure(accountResult.ErrorCode!, accountResult.ErrorMessage!);
            }

            var transactionCount = accountResult.Data!.Transactions.Count;
            return Result<int>.Success(transactionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction count for account {AccountId}", accountId);
            return Result<int>.Failure("GET_TRANSACTION_COUNT_FAILED", ex.Message);
        }
    }
}
