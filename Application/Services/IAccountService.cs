// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.AggregateRoots;
using Shared.Results;

/// <summary>
/// Service interface for account operations and business logic.
/// </summary>
public interface IAccountService
{
    Task<Result<Account>> CreateAccountAsync(string accountNumber, string accountHolder, string currency, decimal initialBalance, CancellationToken cancellationToken = default);
    Task<Result<Account>> GetAccountAsync(string accountId, CancellationToken cancellationToken = default);
    Task<Result> DepositAsync(string accountId, decimal amount, string reference, CancellationToken cancellationToken = default);
    Task<Result> WithdrawAsync(string accountId, decimal amount, string reference, CancellationToken cancellationToken = default);
    Task<Result> CloseAccountAsync(string accountId, string reason, CancellationToken cancellationToken = default);
    Task<Result<List<Account>>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> GetTransactionCountAsync(string accountId, CancellationToken cancellationToken = default);
}
