#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.AggregateRoots;
using Shared.Results;
using Exceptions;

/// <summary>
/// Service interface for account operations and business logic.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="accountNumber">The account number.</param>
    /// <param name="accountHolder">The account holder name.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="initialBalance">The initial balance.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the created account.</returns>
    Task<Result<Account>> CreateAccountAsync(string accountNumber, string accountHolder, string currency, decimal initialBalance, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an account by ID.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the account.</returns>
    Task<Result<Account>> GetAccountAsync(string accountId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deposits money into an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="reference">The transaction reference.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DepositAsync(string accountId, decimal amount, string reference, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Withdraws money from an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="reference">The transaction reference.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> WithdrawAsync(string accountId, decimal amount, string reference, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Closes an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="reason">The reason for closing the account.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> CloseAccountAsync(string accountId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing a list of accounts.</returns>
    Task<Result<List<Account>>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the number of transactions for an account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the transaction count.</returns>
    Task<Result<int>> GetTransactionCountAsync(string accountId, CancellationToken cancellationToken = default);
}
