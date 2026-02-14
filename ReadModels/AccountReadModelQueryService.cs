// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides domain-oriented queries over the <see cref="AccountReadModel"/> store,
/// hiding low-level key lookups behind business-meaningful method signatures.
/// </summary>
/// <remarks>
/// All methods return <see cref="Result{T}"/> so callers can distinguish between a
/// genuine "not found" scenario and an infrastructure failure without relying on exceptions.
/// </remarks>
public interface IAccountReadModelQueryService
{
    /// <summary>
    /// Retrieves a single account read model by its aggregate identifier (store key).
    /// </summary>
    Task<Result<AccountReadModel>> GetByIdAsync(
        string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an account by its human-readable account number (e.g. <c>ACC-0001</c>).
    /// The comparison is case-insensitive.
    /// </summary>
    Task<Result<AccountReadModel>> GetByAccountNumberAsync(
        string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all accounts whose <see cref="AccountReadModel.Status"/> is
    /// <see cref="AccountReadModelStatus.Active"/>.
    /// </summary>
    Task<Result<IReadOnlyList<AccountReadModel>>> GetActiveAccountsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all accounts whose <see cref="AccountReadModel.AccountHolder"/> contains
    /// <paramref name="accountHolder"/> (case-insensitive substring match).
    /// </summary>
    Task<Result<IReadOnlyList<AccountReadModel>>> GetByAccountHolderAsync(
        string accountHolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="count"/> active accounts ordered by
    /// <see cref="AccountReadModel.CurrentBalance"/> descending.
    /// </summary>
    Task<Result<IReadOnlyList<AccountReadModel>>> GetTopBalanceAccountsAsync(
        int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns accounts whose balance falls within the inclusive range
    /// [<paramref name="minBalance"/>, <paramref name="maxBalance"/>].
    /// Only active accounts are included.
    /// </summary>
    Task<Result<IReadOnlyList<AccountReadModel>>> GetByBalanceRangeAsync(
        decimal minBalance, decimal maxBalance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes aggregate statistics over every account currently held in the store.
    /// </summary>
    Task<Result<AccountPortfolioStatistics>> GetPortfolioStatisticsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregate statistics computed over all <see cref="AccountReadModel"/> entries
/// in the store at the time of the query.
/// </summary>
/// <param name="TotalAccounts">Total number of accounts (active + closed).</param>
/// <param name="ActiveAccounts">Number of accounts with <see cref="AccountReadModelStatus.Active"/> status.</param>
/// <param name="ClosedAccounts">Number of accounts with <see cref="AccountReadModelStatus.Closed"/> status.</param>
/// <param name="TotalActiveBalance">Sum of <see cref="AccountReadModel.CurrentBalance"/> across all active accounts.</param>
/// <param name="AverageActiveBalance">Mean balance of active accounts, or zero when none exist.</param>
/// <param name="HighestBalance">Maximum balance held by any single active account, or zero when none exist.</param>
/// <param name="TotalDeposited">Sum of <see cref="AccountReadModel.TotalDeposited"/> across all accounts.</param>
/// <param name="TotalWithdrawn">Sum of <see cref="AccountReadModel.TotalWithdrawn"/> across all accounts.</param>
/// <param name="TotalTransactions">Sum of <see cref="AccountReadModel.TransactionCount"/> across all accounts.</param>
/// <param name="ComputedAt">UTC timestamp when this snapshot was calculated.</param>
public sealed record AccountPortfolioStatistics(
    int TotalAccounts,
    int ActiveAccounts,
    int ClosedAccounts,
    decimal TotalActiveBalance,
    decimal AverageActiveBalance,
    decimal HighestBalance,
    decimal TotalDeposited,
    decimal TotalWithdrawn,
    int TotalTransactions,
    DateTime ComputedAt);

/// <summary>
/// Default implementation of <see cref="IAccountReadModelQueryService"/> backed by
/// <see cref="IReadModelStore{TReadModel}"/>.
/// </summary>
internal sealed class AccountReadModelQueryService : IAccountReadModelQueryService
{
    private readonly IReadModelStore<AccountReadModel> _store;
    private readonly ILogger<AccountReadModelQueryService> _logger;

    /// <summary>Initializes a new <see cref="AccountReadModelQueryService"/>.</summary>
    public AccountReadModelQueryService(
        IReadModelStore<AccountReadModel> store,
        ILogger<AccountReadModelQueryService> logger)
    {
        _store  = GuardClauses.NotNull(store,  nameof(store));
        _logger = GuardClauses.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<AccountReadModel>> GetByIdAsync(
        string accountId, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(accountId, nameof(accountId));

        var result = await _store.GetAsync(accountId, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogDebug("Account read model not found for id '{AccountId}'.", accountId);

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<AccountReadModel>> GetByAccountNumberAsync(
        string accountNumber, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(accountNumber, nameof(accountNumber));

        var query = await _store.QueryAsync(
            m => m.AccountNumber.Equals(accountNumber, StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        if (!query.IsSuccess)
            return Result<AccountReadModel>.Failure(query.ErrorCode!, query.ErrorMessage!);

        var match = query.Data!.FirstOrDefault();

        return match is not null
            ? Result<AccountReadModel>.Success(match)
            : Result<AccountReadModel>.Failure(
                "READ_MODEL_NOT_FOUND",
                $"No account found with account number '{accountNumber}'.");
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountReadModel>>> GetActiveAccountsAsync(
        CancellationToken cancellationToken = default) =>
        _store.QueryAsync(m => m.Status == AccountReadModelStatus.Active, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountReadModel>>> GetByAccountHolderAsync(
        string accountHolder, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(accountHolder, nameof(accountHolder));

        return _store.QueryAsync(
            m => m.AccountHolder.Contains(accountHolder, StringComparison.OrdinalIgnoreCase),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AccountReadModel>>> GetTopBalanceAccountsAsync(
        int count, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNegative(count, nameof(count));

        var active = await _store.QueryAsync(
            m => m.Status == AccountReadModelStatus.Active, cancellationToken);

        if (!active.IsSuccess)
            return active;

        IReadOnlyList<AccountReadModel> top = active.Data!
            .OrderByDescending(m => m.CurrentBalance)
            .Take(count)
            .ToList();

        return Result<IReadOnlyList<AccountReadModel>>.Success(top);
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountReadModel>>> GetByBalanceRangeAsync(
        decimal minBalance, decimal maxBalance, CancellationToken cancellationToken = default) =>
        _store.QueryAsync(
            m => m.Status == AccountReadModelStatus.Active
              && m.CurrentBalance >= minBalance
              && m.CurrentBalance <= maxBalance,
            cancellationToken);

    /// <inheritdoc />
    public async Task<Result<AccountPortfolioStatistics>> GetPortfolioStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var all = await _store.GetAllAsync(cancellationToken);

        if (!all.IsSuccess)
            return Result<AccountPortfolioStatistics>.Failure(all.ErrorCode!, all.ErrorMessage!);

        var accounts = all.Data!;
        var active   = accounts.Where(a => a.Status == AccountReadModelStatus.Active).ToList();
        var closed   = accounts.Where(a => a.Status == AccountReadModelStatus.Closed).ToList();

        var stats = new AccountPortfolioStatistics(
            TotalAccounts:        accounts.Count,
            ActiveAccounts:       active.Count,
            ClosedAccounts:       closed.Count,
            TotalActiveBalance:   active.Count > 0 ? active.Sum(a => a.CurrentBalance)     : 0m,
            AverageActiveBalance: active.Count > 0 ? active.Average(a => a.CurrentBalance) : 0m,
            HighestBalance:       active.Count > 0 ? active.Max(a => a.CurrentBalance)     : 0m,
            TotalDeposited:       accounts.Sum(a => a.TotalDeposited),
            TotalWithdrawn:       accounts.Sum(a => a.TotalWithdrawn),
            TotalTransactions:    accounts.Sum(a => a.TransactionCount),
            ComputedAt:           DateTime.UtcNow);

        return Result<AccountPortfolioStatistics>.Success(stats);
    }
}
