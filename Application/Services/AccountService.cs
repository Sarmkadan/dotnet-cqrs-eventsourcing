// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.AggregateRoots;
using Data.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Results;

/// <summary>
/// Account service implementation handling account operations and persistence.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IRepository<Account> _accountRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IRepository<Account> accountRepository, IEventBus eventBus, ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Account>> CreateAccountAsync(string accountNumber, string accountHolder,
        string currency, decimal initialBalance, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = new Account();
            account.CreateAccount(accountNumber, accountHolder, currency, initialBalance);

            var saveResult = await _accountRepository.SaveAsync(account, cancellationToken);
            if (!saveResult.IsSuccess)
                return Result<Account>.Failure(saveResult.ErrorCode!, saveResult.ErrorMessage!);

            // Publish events
            await _eventBus.PublishEventsAsync(
                account.GetUncommittedEvents().Cast<Domain.Events.DomainEvent>().ToList(),
                cancellationToken
            );

            _logger.LogInformation("Account created: {AccountNumber} for {AccountHolder}", accountNumber, accountHolder);
            return Result<Account>.Success(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return Result<Account>.Failure("CREATE_ACCOUNT_FAILED", ex.Message);
        }
    }

    public async Task<Result<Account>> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
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

    public async Task<Result> DepositAsync(string accountId, decimal amount, string reference, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountResult = await GetAccountAsync(accountId, cancellationToken);
            if (!accountResult.IsSuccess)
                return Result.Failure(accountResult.ErrorCode!, accountResult.ErrorMessage!);

            var account = accountResult.Data!;
            account.Deposit(amount, reference);

            var saveResult = await _accountRepository.SaveAsync(account, cancellationToken);
            if (!saveResult.IsSuccess)
                return saveResult;

            // Publish events
            await _eventBus.PublishEventsAsync(
                account.GetUncommittedEvents().Cast<Domain.Events.DomainEvent>().ToList(),
                cancellationToken
            );

            _logger.LogInformation("Deposit processed: {Amount} to account {AccountId}", amount, accountId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deposit to account {AccountId}", accountId);
            return Result.Failure("DEPOSIT_FAILED", ex.Message);
        }
    }

    public async Task<Result> WithdrawAsync(string accountId, decimal amount, string reference, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountResult = await GetAccountAsync(accountId, cancellationToken);
            if (!accountResult.IsSuccess)
                return Result.Failure(accountResult.ErrorCode!, accountResult.ErrorMessage!);

            var account = accountResult.Data!;
            account.Withdraw(amount, reference);

            var saveResult = await _accountRepository.SaveAsync(account, cancellationToken);
            if (!saveResult.IsSuccess)
                return saveResult;

            // Publish events
            await _eventBus.PublishEventsAsync(
                account.GetUncommittedEvents().Cast<Domain.Events.DomainEvent>().ToList(),
                cancellationToken
            );

            _logger.LogInformation("Withdrawal processed: {Amount} from account {AccountId}", amount, accountId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing withdrawal from account {AccountId}", accountId);
            return Result.Failure("WITHDRAWAL_FAILED", ex.Message);
        }
    }

    public async Task<Result> CloseAccountAsync(string accountId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountResult = await GetAccountAsync(accountId, cancellationToken);
            if (!accountResult.IsSuccess)
                return Result.Failure(accountResult.ErrorCode!, accountResult.ErrorMessage!);

            var account = accountResult.Data!;
            account.CloseAccount(reason);

            var saveResult = await _accountRepository.SaveAsync(account, cancellationToken);
            if (!saveResult.IsSuccess)
                return saveResult;

            // Publish events
            await _eventBus.PublishEventsAsync(
                account.GetUncommittedEvents().Cast<Domain.Events.DomainEvent>().ToList(),
                cancellationToken
            );

            _logger.LogInformation("Account closed: {AccountId} - Reason: {Reason}", accountId, reason);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing account {AccountId}", accountId);
            return Result.Failure("CLOSE_ACCOUNT_FAILED", ex.Message);
        }
    }

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

    public async Task<Result<int>> GetTransactionCountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountResult = await GetAccountAsync(accountId, cancellationToken);
            if (!accountResult.IsSuccess)
                return Result<int>.Failure(accountResult.ErrorCode!, accountResult.ErrorMessage!);

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
