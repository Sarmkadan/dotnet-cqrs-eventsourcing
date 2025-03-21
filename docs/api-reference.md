# API Reference

Complete API documentation for the CQRS + Event Sourcing framework.

## Core Interfaces

### IAccountService

Main service for account operations.

```csharp
public interface IAccountService
{
    /// Creates a new account with initial balance
    Task<Result<Account>> CreateAccountAsync(
        string accountId,
        string accountHolderName,
        string currency,
        decimal initialBalance,
        CancellationToken cancellationToken = default);

    /// Deposits money into account
    Task<Result<decimal>> DepositAsync(
        string accountId,
        decimal amount,
        string referenceNumber,
        CancellationToken cancellationToken = default);

    /// Withdraws money from account
    Task<Result<decimal>> WithdrawAsync(
        string accountId,
        decimal amount,
        string referenceNumber,
        CancellationToken cancellationToken = default);

    /// Retrieves current account state
    Task<Result<Account>> GetAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// Closes account
    Task<Result<bool>> CloseAccountAsync(
        string accountId,
        string reason,
        CancellationToken cancellationToken = default);
}
```

#### CreateAccountAsync

Creates and persists a new account.

**Parameters:**
- `accountId` (string): Unique account identifier
- `accountHolderName` (string): Name of account holder
- `currency` (string): Currency code (e.g., "USD", "EUR")
- `initialBalance` (decimal): Starting balance

**Returns:** `Result<Account>` containing created account

**Example:**
```csharp
var result = await accountService.CreateAccountAsync(
    "ACC-2024-001",
    "Alice Johnson",
    "USD",
    5000m
);

if (result.IsSuccess)
{
    Console.WriteLine($"Created: {result.Data.Id}");
}
```

#### DepositAsync

Adds funds to account.

**Parameters:**
- `accountId` (string): Target account
- `amount` (decimal): Amount to deposit (must be positive)
- `referenceNumber` (string): Transaction reference

**Returns:** `Result<decimal>` containing new balance

**Raises:** `DomainException` if amount invalid

**Example:**
```csharp
var result = await accountService.DepositAsync("ACC-001", 500m, "DEP-001");
if (result.IsSuccess)
{
    Console.WriteLine($"New balance: {result.Data}");
}
```

#### WithdrawAsync

Removes funds from account.

**Parameters:**
- `accountId` (string): Source account
- `amount` (decimal): Amount to withdraw (must be positive)
- `referenceNumber` (string): Transaction reference

**Returns:** `Result<decimal>` containing new balance

**Raises:** `DomainException` if insufficient funds or invalid amount

**Example:**
```csharp
var result = await accountService.WithdrawAsync("ACC-001", 200m, "WTH-001");
if (!result.IsSuccess)
{
    if (result.Error == "InsufficientFunds")
        Console.WriteLine("Not enough balance");
}
```

#### GetAccountAsync

Retrieves current account state.

**Parameters:**
- `accountId` (string): Account to retrieve

**Returns:** `Result<Account>` containing account state

**Example:**
```csharp
var result = await accountService.GetAccountAsync("ACC-001");
if (result.IsSuccess)
{
    var account = result.Data;
    Console.WriteLine($"Balance: {account.Balance.CurrentAmount}");
    Console.WriteLine($"Transactions: {account.Transactions.Count}");
}
```

### IEventStore

Manages event persistence and retrieval.

```csharp
public interface IEventStore
{
    /// Persists new events with optimistic concurrency control
    Task SaveEventsAsync(
        string aggregateId,
        int expectedVersion,
        IEnumerable<DomainEvent> uncommittedEvents,
        CancellationToken cancellationToken = default);

    /// Retrieves all events for aggregate
    Task<List<EventEnvelope>> GetEventsAsync(
        string aggregateId,
        int fromVersion = 0,
        CancellationToken cancellationToken = default);

    /// Retrieves single event by version
    Task<EventEnvelope?> GetEventAsync(
        string aggregateId,
        int version,
        CancellationToken cancellationToken = default);

    /// Retrieves all events from all aggregates
    Task<List<EventEnvelope>> GetAllEventsAsync(
        CancellationToken cancellationToken = default);
}
```

#### SaveEventsAsync

Persists events with version checking.

**Parameters:**
- `aggregateId` (string): Aggregate identifier
- `expectedVersion` (int): Expected current version for concurrency check
- `uncommittedEvents` (IEnumerable<DomainEvent>): New events to persist

**Raises:** `OptimisticConcurrencyException` if version mismatch

**Example:**
```csharp
try
{
    await eventStore.SaveEventsAsync(
        "ACC-001",
        3, // Expected current version
        new[] { new MoneyDeposited("ACC-001", 4, 500m, "ref", DateTime.UtcNow) }
    );
}
catch (OptimisticConcurrencyException)
{
    Console.WriteLine("Version conflict - implement retry logic");
}
```

#### GetEventsAsync

Retrieves event stream.

**Parameters:**
- `aggregateId` (string): Aggregate identifier
- `fromVersion` (int): Start from version (0 = all)

**Returns:** `List<EventEnvelope>` ordered by version

**Example:**
```csharp
var events = await eventStore.GetEventsAsync("ACC-001", fromVersion: 10);
Console.WriteLine($"Retrieved {events.Count} events from version 10 onward");
```

#### GetEventAsync

Retrieves specific event.

**Parameters:**
- `aggregateId` (string): Aggregate identifier
- `version` (int): Event version

**Returns:** `EventEnvelope` or null if not found

**Example:**
```csharp
var @event = await eventStore.GetEventAsync("ACC-001", 5);
if (@event != null)
{
    Console.WriteLine($"Event type: {@event.EventType}");
}
```

### IEventBus

Pub/sub messaging for events.

```csharp
public interface IEventBus
{
    /// Publishes event to all subscribers
    Task PublishAsync<T>(
        T @event,
        CancellationToken cancellationToken = default)
        where T : IDomainEvent;

    /// Subscribes to event type
    Task SubscribeAsync<T>(
        Func<T, Task> handler,
        CancellationToken cancellationToken = default)
        where T : IDomainEvent;

    /// Unsubscribes from event type
    Task UnsubscribeAsync<T>(
        CancellationToken cancellationToken = default)
        where T : IDomainEvent;
}
```

#### PublishAsync

Publishes event to all subscribed handlers.

**Parameters:**
- `event` (T): Event instance
- `cancellationToken`: Cancellation token

**Example:**
```csharp
var @event = new MoneyDeposited("ACC-001", 2, 500m, "ref", DateTime.UtcNow);
await eventBus.PublishAsync(@event);
```

#### SubscribeAsync

Registers handler for event type.

**Parameters:**
- `handler` (Func<T, Task>): Async handler function

**Example:**
```csharp
await eventBus.SubscribeAsync<AccountCreated>(async (evt) =>
{
    Console.WriteLine($"Account {evt.AggregateId} created");
    await Task.CompletedTask;
});
```

### IProjectionService

Builds and manages read models.

```csharp
public interface IProjectionService
{
    /// Builds projection for specific aggregate
    Task<AccountProjection> BuildProjectionAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    /// Rebuilds all projections from events
    Task RebuildProjectionsAsync(
        CancellationToken cancellationToken = default);

    /// Clears projection for aggregate
    Task ClearProjectionAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
```

#### BuildProjectionAsync

Builds optimized read model.

**Parameters:**
- `accountId` (string): Aggregate identifier

**Returns:** `AccountProjection` containing aggregate summary

**Example:**
```csharp
var projection = await projectionService.BuildProjectionAsync("ACC-001");
Console.WriteLine($"Total deposits: {projection.TotalDeposits}");
Console.WriteLine($"Total withdrawals: {projection.TotalWithdrawals}");
```

#### RebuildProjectionsAsync

Rebuilds all projections from event stream.

**Use cases:**
- After code changes to projection logic
- To recover from corrupted read models
- After migration changes

**Example:**
```csharp
// Long-running operation
await projectionService.RebuildProjectionsAsync();
Console.WriteLine("Projections rebuilt");
```

### ISnapshotService

Optimizes performance with snapshots.

```csharp
public interface ISnapshotService
{
    /// Creates snapshot of aggregate state
    Task CreateSnapshotAsync(
        Account aggregate,
        string aggregateId,
        int version,
        CancellationToken cancellationToken = default);

    /// Retrieves latest snapshot
    Task<AggregateSnapshot?> GetSnapshotAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);

    /// Deletes snapshot
    Task DeleteSnapshotAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);
}
```

#### CreateSnapshotAsync

Creates snapshot for performance optimization.

**Parameters:**
- `aggregate` (Account): Current aggregate state
- `aggregateId` (string): Aggregate identifier
- `version` (int): Current version number

**Example:**
```csharp
// Create snapshot every 100 events
if (eventCount % 100 == 0)
{
    await snapshotService.CreateSnapshotAsync(account, "ACC-001", eventCount);
}
```

#### GetSnapshotAsync

Retrieves latest snapshot.

**Returns:** `AggregateSnapshot` or null if none exists

**Example:**
```csharp
var snapshot = await snapshotService.GetSnapshotAsync("ACC-001");
if (snapshot != null)
{
    var account = snapshot.RestoreAggregate();
    // Only replay events after snapshot version
}
```

## Domain Models

### Account

Represents bank account aggregate root.

```csharp
public class Account : AggregateRoot
{
    public string Id { get; private set; }
    public string AccountHolderName { get; private set; }
    public Balance Balance { get; private set; }
    public List<Transaction> Transactions { get; private set; }
    public bool IsClosed { get; private set; }
    public int Version { get; private set; }
}
```

**Properties:**
- `Id`: Unique account identifier
- `AccountHolderName`: Name of account holder
- `Balance`: Current balance value object
- `Transactions`: List of transactions
- `IsClosed`: Account closed status
- `Version`: Current event stream version

### Money

Immutable value object for currency amounts.

```csharp
public class Money : IEquatable<Money>
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative");
        Amount = amount;
        Currency = currency;
    }
}
```

**Operations:**
```csharp
var money1 = new Money(100m, "USD");
var money2 = new Money(50m, "USD");

var sum = money1.Add(money2);      // 150 USD
var diff = money1.Subtract(money2); // 50 USD
bool equal = money1 == money2;      // false
```

### Balance

Represents account balance.

```csharp
public class Balance
{
    public decimal CurrentAmount { get; private set; }
    public decimal HeldAmount { get; private set; }
    public string Currency { get; private set; }
    public decimal AvailableAmount => CurrentAmount - HeldAmount;
}
```

**Properties:**
- `CurrentAmount`: Total funds
- `HeldAmount`: Funds on hold
- `AvailableAmount`: Available for withdrawal
- `Currency`: Currency code

### Transaction

Single transaction record.

```csharp
public class Transaction
{
    public decimal Amount { get; private set; }
    public string Type { get; private set; } // "Deposit" or "Withdrawal"
    public string Reference { get; private set; }
    public DateTime Timestamp { get; private set; }
}
```

## Events

### DomainEvent

Base class for all domain events.

```csharp
public abstract class DomainEvent : IDomainEvent
{
    public string AggregateId { get; protected set; }
    public int Version { get; protected set; }
    public DateTime Timestamp { get; protected set; }
    public string EventType => GetType().Name;
}
```

### Account Events

#### AccountCreated

Fired when account is created.

```csharp
public class AccountCreated : DomainEvent
{
    public string AccountHolderName { get; set; }
    public string Currency { get; set; }
    public decimal InitialBalance { get; set; }
}
```

#### MoneyDeposited

Fired when funds are deposited.

```csharp
public class MoneyDeposited : DomainEvent
{
    public decimal Amount { get; set; }
    public string Reference { get; set; }
}
```

#### MoneyWithdrawn

Fired when funds are withdrawn.

```csharp
public class MoneyWithdrawn : DomainEvent
{
    public decimal Amount { get; set; }
    public string Reference { get; set; }
}
```

#### AccountClosed

Fired when account is closed.

```csharp
public class AccountClosed : DomainEvent
{
    public string Reason { get; set; }
}
```

## Result Pattern

### Result

Generic result wrapper for error handling.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    
    public static Result<T> Success(T data) 
        => new() { IsSuccess = true, Data = data };
    
    public static Result<T> Failure(string error) 
        => new() { IsSuccess = false, Error = error };
}
```

**Usage:**
```csharp
var result = await service.DoSomethingAsync();

if (result.IsSuccess)
{
    UseData(result.Data);
}
else
{
    HandleError(result.Error);
}
```

## Exceptions

### DomainException

Domain-level validation errors.

```csharp
throw new DomainException("Insufficient funds");
```

### OptimisticConcurrencyException

Version conflict in event store.

```csharp
catch (OptimisticConcurrencyException)
{
    // Reload and retry
}
```

### AggregateNotFoundException

Aggregate not found.

```csharp
catch (AggregateNotFoundException)
{
    // Handle missing aggregate
}
```

## Extension Methods

### CommandExtensions

```csharp
public static class CommandExtensions
{
    public static IServiceCollection AddCommandHandlers(
        this IServiceCollection services)
    {
        // Registers all command handlers
        return services;
    }
}
```

## Configuration

### AddCqrsFramework

Registers all framework services.

```csharp
services.AddCqrsFramework();
```

Registers:
- `IEventStore` -> `EventStore`
- `IEventBus` -> `EventBus`
- `IEventRepository` -> `InMemoryEventRepository`
- `IAccountService` -> `AccountService`
- `IProjectionService` -> `ProjectionService`
- `ISnapshotService` -> `SnapshotService`

### Custom Configuration

```csharp
services.AddCqrsFramework();
services.AddSingleton<IEventRepository, 
    CustomSqlEventRepository>();
services.AddSingleton<ICacheService, 
    RedisCacheService>();
```

## Complete Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.Events;

// Setup
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Subscribe to events
await eventBus.SubscribeAsync<AccountCreated>(async (evt) =>
{
    Console.WriteLine($"✓ Account created: {evt.AggregateId}");
    await Task.CompletedTask;
});

// Create account
var createResult = await accountService.CreateAccountAsync(
    "ACC-001", "John", "USD", 1000m
);

// Perform transactions
if (createResult.IsSuccess)
{
    await accountService.DepositAsync("ACC-001", 500m, "DEP-001");
    await accountService.WithdrawAsync("ACC-001", 200m, "WTH-001");
}

// Query state
var account = await accountService.GetAccountAsync("ACC-001");
Console.WriteLine($"Balance: {account.Data.Balance.CurrentAmount}");

// View events
var events = await eventStore.GetEventsAsync("ACC-001");
foreach (var evt in events)
{
    Console.WriteLine($"Event: {evt.EventType}");
}
```

---

For more details, see the example programs in the `examples/` directory.
