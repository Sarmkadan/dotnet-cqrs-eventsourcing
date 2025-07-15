# AccountProjectionSummary

Represents a read-optimized snapshot of an account's financial state at a specific point in time. This type is used in CQRS query pathways to provide pre-aggregated balance, transaction counts, and activity timestamps without replaying the full event stream.

## API

### `Status`

`public string Status`

Gets the current operational status of the account (e.g., "Active", "Closed", "Suspended"). The value is derived from the latest status-related domain event applied to this projection.

### `CurrentBalance`

`public decimal CurrentBalance`

Gets the net balance of the account after all deposits and withdrawals have been applied. This value is maintained incrementally as events are projected and reflects the authoritative balance for query purposes.

### `TotalDeposits`

`public decimal TotalDeposits`

Gets the cumulative sum of all deposit amounts applied to the account since its creation. This is a running total used for reporting and analytics without scanning individual transactions.

### `TotalWithdrawals`

`public decimal TotalWithdrawals`

Gets the cumulative sum of all withdrawal amounts applied to the account since its creation. Together with `TotalDeposits`, it provides a high-level view of cash flow activity.

### `TransactionCount`

`public int TransactionCount`

Gets the total number of financial transactions (deposits, withdrawals, and any other balance-affecting operations) that have been processed for this account. Incremented atomically with each applied event.

### `LastUpdated`

`public DateTime LastUpdated`

Gets the timestamp of the most recent event that modified this projection. Set to the event's occurrence time, not the system processing time, ensuring consistency with the event log.

## Usage

### Retrieving an Account Summary from a Query Handler

```csharp
public async Task<AccountProjectionSummary> GetAccountSummaryAsync(
    Guid accountId,
    CancellationToken cancellationToken)
{
    // The query service fetches the pre-built projection from the read store.
    var summary = await _readStore
        .FindAsync<AccountProjectionSummary>(accountId, cancellationToken)
        .ConfigureAwait(false);

    if (summary is null)
        throw new NotFoundException($"Account {accountId} not found.");

    return summary;
}
```

### Checking Account Health Before Processing a Command

```csharp
public async Task<bool> CanWithdrawAsync(
    Guid accountId,
    decimal requestedAmount,
    CancellationToken cancellationToken)
{
    var summary = await GetAccountSummaryAsync(accountId, cancellationToken);

    if (summary.Status != "Active")
        return false;

    if (summary.CurrentBalance < requestedAmount)
        return false;

    // Additional business rule: reject if transaction volume is suspiciously high.
    if (summary.TransactionCount > 10_000 && requestedAmount > summary.CurrentBalance * 0.8m)
        return false;

    return true;
}
```

## Notes

- **Eventual Consistency**: The values in this projection reflect the state as of the last successfully applied event. In distributed deployments, a brief delay may exist between command-side writes and projection updates.
- **Thread Safety**: This type is a plain data object intended for read-only consumption. Instances are typically produced by a single-threaded projection handler and published to query stores. Concurrent read access is safe; concurrent mutation by multiple writers is not supported and will produce undefined results.
- **Overflow**: `TotalDeposits` and `TotalWithdrawals` are `decimal` with no explicit upper bound. Extremely high-volume accounts over long lifetimes may approach the limits of the type, though this is unlikely in typical financial domains.
- **Status Transitions**: The `Status` field reflects only the most recent status event. If an account is closed and later reopened, the field will show "Active" again; it does not retain historical status changes.
- **Timestamp Precision**: `LastUpdated` uses `DateTime`, which has system-clock dependency. In scenarios requiring strict monotonic ordering, correlate this value with the event stream position rather than relying on wall-clock time alone.
