// existing content ...

## AccountAggregateTests

The `AccountAggregateTests` class provides a comprehensive set of unit tests for the `Account` aggregate root, covering various scenarios such as account creation, deposit, withdrawal, and closure. These tests ensure that the `Account` class behaves correctly under different conditions.

Example usage:
```csharp
public class Program
{
  public static void Main(string[] args) 
  {
    var test = new AccountAggregateTests();

    // Test creating an account with valid parameters
    test.CreateAccount_ValidParameters_RaisesAccountCreatedEvent();

    // Test depositing a positive amount
    var account = AccountAggregateTests.CreateFreshAccount();
    account.Deposit(100m, "REF-001");
    // Verify account state...

    // Test withdrawing with sufficient funds
    account.Withdraw(50m, "REF-002");
    // Verify account state...
  }
}
```

## AccountServiceTests

The `AccountServiceTests` class contains integration‑style unit tests for the `AccountService` application service. It validates that account creation, deposits, withdrawals, closures, and related error handling work correctly by exercising the service with mocked dependencies.

Example usage:
```csharp
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Tests.Application;

public class Program
{
  public static async Task Main(string[] args) 
  {
    var tests = new AccountServiceTests();

    // Run a few representative test methods manually
    await tests.CreateAccountAsync_ValidParameters_ReturnsSuccessWithAccount();
    await tests.DepositAsync_ValidAccount_SavesAndPublishesEvents();
    await tests.WithdrawAsync_InsufficientFunds_ReturnsFailure();
    await tests.CloseAccountAsync_ValidAccount_SucceedsAndPublishesClosedEvent();
  }
}
```

## EventStoreCompactionServiceTests

The `EventStoreCompactionServiceTests` class provides unit tests for the `EventStoreCompactionService`, which handles event store compaction by removing old events while preserving snapshots. These tests verify compaction behavior under various scenarios including version-based compaction, snapshot-based compaction, and error handling when snapshots are missing.

Example usage:
```csharp
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Tests.Application;
using DotNetCqrsEventSourcing.Application.Services;

public class Program
{
  public static async Task Main(string[] args)
  {
    var tests = new EventStoreCompactionServiceTests();

    // Compact events to a specific version (keep events from version 4 onwards)
    var result1 = await tests.CompactToVersionAsync_RemovesEventsBeforeVersion();
    Console.WriteLine($"Compacted to version {result1.Data?.CompactedToVersion}, removed {result1.Data?.EventsRemoved} events");

    // Compact using the latest snapshot version as the compaction point
    var result2 = await tests.CompactAsync_WithSnapshot_UsesSnapshotVersion();
    Console.WriteLine($"Snapshot-based compaction removed {result2.Data?.EventsRemoved} events");

    // Attempt compaction when no snapshot exists (should fail gracefully)
    var result3 = await tests.CompactAsync_NoSnapshot_ReturnsFailure();
    if (!result3.IsSuccess) Console.WriteLine($"Compaction failed: {result3.ErrorCode}");

    // Compact multiple aggregates, skipping those without snapshots
    var result4 = await tests.CompactAllAsync_SkipsAggregatesWithoutSnapshots();
    Console.WriteLine($"Successfully compacted {result4.Data?.Count} aggregate(s)");

    // Compact to an invalid version (should return failure)
    var result5 = await tests.CompactToVersionAsync_InvalidVersion_ReturnsFailure();
    if (!result5.IsSuccess) Console.WriteLine($"Invalid version: {result5.ErrorCode}");
  }
}
```