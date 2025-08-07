#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using Domain.AggregateRoots;
using Shared.Enums;
using Shared.Results;
using System.Threading;

/// <summary>
/// Extension methods for <see cref="AccountRepository"/> providing additional functionality
/// for account management operations.
/// </summary>
public static class AccountRepositoryExtensions
{
    /// <summary>
    /// Gets an account by its ID or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="repository">The account repository instance.</param>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="id"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <returns>The existing or newly created account wrapped in a <see cref="Result{T}"/>.</returns>
    public static async Task<Result<Account>> GetOrCreateAsync(
        this AccountRepository repository,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var existingAccountResult = await repository.GetByIdAsync(id, cancellationToken);

        if (existingAccountResult.IsSuccess)
            return existingAccountResult;

        // Create new account if not found
        var newAccount = new Account(id);
        var saveResult = await repository.SaveAsync(newAccount, cancellationToken);

        if (!saveResult.IsSuccess)
            return Result<Account>.Failure("CREATE_FAILED", saveResult.ErrorMessage ?? "Failed to save the new account.");

        return Result<Account>.Success(newAccount);
    }

    /// <summary>
    /// Gets all accounts with optional filtering by account status.
    /// </summary>
    /// <param name="repository">The account repository instance.</param>
    /// <param name="accountStatus">Optional account status to filter by (null for all).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <returns>List of accounts matching the filter wrapped in a <see cref="Result{T}"/>.</returns>
    public static async Task<Result<List<Account>>> GetAllByStatusAsync(
        this AccountRepository repository,
        AggregateStatus? accountStatus = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var allAccountsResult = await repository.GetAllAsync(cancellationToken);

        if (!allAccountsResult.IsSuccess)
            return Result<List<Account>>.Failure(
                allAccountsResult.ErrorCode ?? "GET_ALL_FAILED",
                allAccountsResult.ErrorMessage ?? "Failed to load accounts.");

        var filteredAccounts = allAccountsResult.Data!;

        if (accountStatus.HasValue)
            filteredAccounts = filteredAccounts.Where(a => a.Status == accountStatus.Value).ToList();

        return Result<List<Account>>.Success(filteredAccounts);
    }

    /// <summary>
    /// Checks if an account exists and returns the account if it does.
    /// </summary>
    /// <param name="repository">The account repository instance.</param>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="id"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <returns>Result containing the account if it exists, or failure if not found.</returns>
    public static async Task<Result<Account>> GetIfExistsAsync(
        this AccountRepository repository,
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var exists = await repository.ExistsAsync(id, cancellationToken);

        if (!exists)
            return Result<Account>.Failure("NOT_FOUND", $"Account {id} does not exist");

        return await repository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Transfers balance between two accounts atomically.
    /// </summary>
    /// <param name="repository">The account repository instance.</param>
    /// <param name="fromAccountId">Source account ID.</param>
    /// <param name="toAccountId">Destination account ID.</param>
    /// <param name="amount">Amount to transfer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="fromAccountId"/> or <paramref name="toAccountId"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <returns>Result indicating success or failure of the transfer.</returns>
    public static async Task<Result> TransferBalanceAsync(
        this AccountRepository repository,
        string fromAccountId,
        string toAccountId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromAccountId, nameof(fromAccountId));
        ArgumentException.ThrowIfNullOrWhiteSpace(toAccountId, nameof(toAccountId));

        if (amount <= 0)
            return Result.Failure("INVALID_AMOUNT", "Transfer amount must be positive");

        // Get source account
        var fromAccountResult = await repository.GetByIdAsync(fromAccountId, cancellationToken);
        if (!fromAccountResult.IsSuccess)
            return Result.Failure(
                fromAccountResult.ErrorCode ?? "SOURCE_ACCOUNT_NOT_FOUND",
                fromAccountResult.ErrorMessage ?? $"Failed to load source account {fromAccountId}.");

        var fromAccount = fromAccountResult.Data!;

        // Get destination account
        var toAccountResult = await repository.GetByIdAsync(toAccountId, cancellationToken);
        if (!toAccountResult.IsSuccess)
            return Result.Failure(
                toAccountResult.ErrorCode ?? "DESTINATION_ACCOUNT_NOT_FOUND",
                toAccountResult.ErrorMessage ?? $"Failed to load destination account {toAccountId}.");

        var toAccount = toAccountResult.Data!;

        // Check sufficient balance
        if (fromAccount.Balance.CurrentAmount.Amount < amount)
            return Result.Failure("INSUFFICIENT_FUNDS", "Source account has insufficient balance");

        // Perform withdrawal from source account
        fromAccount.Withdraw(amount, "TRANSFER_OUT");
        var withdrawResult = await repository.SaveAsync(fromAccount, cancellationToken);
        if (!withdrawResult.IsSuccess)
            return withdrawResult;

        // Perform deposit to destination account
        toAccount.Deposit(amount, "TRANSFER_IN");
        var depositResult = await repository.SaveAsync(toAccount, cancellationToken);
        if (!depositResult.IsSuccess)
        {
            // Rollback withdrawal if deposit fails
            fromAccount.Deposit(amount, "TRANSFER_ROLLBACK");
            var rollbackResult = await repository.SaveAsync(fromAccount, cancellationToken);

            if (!rollbackResult.IsSuccess)
                return Result.Failure(
                    "TRANSFER_ROLLBACK_FAILED",
                    $"Deposit to {toAccountId} failed ({depositResult.ErrorMessage}) and the withdrawal " +
                    $"rollback on {fromAccountId} also failed ({rollbackResult.ErrorMessage}). " +
                    "Manual reconciliation is required.");

            return depositResult;
        }

        return Result.Success();
    }
}