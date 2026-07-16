# dotnet-cqrs-eventsourcing

CQRS + Event Sourcing framework in C# with a banking `Account` aggregate as the reference domain.

## Architecture

The full picture - layers, write/read data flow, projection engine, snapshots/compaction, extension points and known limitations - lives in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Design decisions are recorded as ADRs in [docs/adr/](docs/adr/). Short version:

- `Domain/` - aggregates, events, value objects (no infrastructure dependencies)
- `Application/` - event store, event bus, services, sagas
- `ReadModels/` - projection engine with retries, checkpointing and dead-lettering
- `Infrastructure/` - dispatch, workers, middleware, CLI
- All default stores are in-memory; swap `IEventRepository` / `IReadModelStore<T>` for real persistence.

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

## TestSaga

The `TestSaga` class is a minimal saga implementation used exclusively for testing purposes. It extends `SagaBase` and demonstrates core saga behavior including state transitions, event handling, and correlation management. The class tracks the number of events processed through the `HandledEvents` property, making it ideal for verifying saga lifecycle and handler logic in unit tests.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Application.Sagas;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Tests.Application.Sagas;
using DotNetCqrsEventSourcing.Shared.Results;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Create a new saga instance
        var saga = new TestSaga();
        Console.WriteLine($"Initial state: {{saga.State}}"); // NotStarted

        // Set correlation ID for tracking
        saga.SetCorrelation("account-123");

        // Handle an event to activate the saga
        var accountCreatedEvent = new AccountCreatedEvent("agg-1", "ACC-123", "Test User", "USD", 1000m)
        {
            CorrelationId = "account-123"
        };

        saga.Handle(accountCreatedEvent);
        Console.WriteLine($"After handling event: {{saga.State}}, HandledEvents = {{saga.HandledEvents}}"); // Active, 1

        // Mark saga as completed
        saga.Finish();
        Console.WriteLine($"After completion: {{saga.State}}"); // Completed

        // Create saga with correlation ID in constructor
        var saga2 = new TestSaga("corr-456");
        Console.WriteLine($"Saga with correlation: {{saga2.CorrelationId}}"); // corr-456

        // Use with saga handler and repository
        var repository = new InMemorySagaRepository<TestSaga>();
        var handler = new TestSagaHandler(repository);

        var result = await handler.HandleAsync(accountCreatedEvent);
        if (result.IsSuccess)
        {
            Console.WriteLine("Saga persisted successfully");
        }
    }
}
```