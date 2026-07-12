![Build](https://github.com/sarmkadan/dotnet-cqrs-eventsourcing/actions/workflows/build.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/dotnet-cqrs-eventsourcing)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

# dotnet-cqrs-eventsourcing

**Production-Grade CQRS + Event Sourcing Framework for .NET 10**

A complete, enterprise-ready implementation of Command Query Responsibility Segregation (CQRS) and Event Sourcing patterns for modern .NET applications. Built with type safety, async-first design, and production concerns in mind.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Key Features](#key-features)
4. [Installation](#installation)
5. [Quick Start](#quick-start)
6. [Usage Examples](#usage-examples)
7. [API Reference](#api-reference)
8. [Configuration](#configuration)
9. [Deployment](#deployment)
10. [Testing](#testing)
11. [Performance](#performance)
12. [Troubleshooting](#troubleshooting)
13. [Related Projects](#related-projects)
14. [Contributing](#contributing)
15. [License](#license)

## Overview

### What is CQRS?

Command Query Responsibility Segregation (CQRS) is an architectural pattern that separates read and write operations into different models. Commands modify state, while queries retrieve state. This separation allows:

- **Independent Scaling**: Read and write models can scale independently
- **Optimized Data Access**: Each model optimized for its specific purpose
- **Simplified Logic**: Separation of concerns between commands and queries
- **Event Traceability**: Complete audit trail of all state changes

### What is Event Sourcing?

Event Sourcing persists the state of an entity as a series of state-changing events instead of storing the current state directly. Benefits include:

- **Complete History**: Full audit trail of every change
- **Replay Capability**: Reconstruct any past state
- **Debugging**: Exact reproduction of past issues
- **Compliance**: Meet regulatory requirements for immutable records
- **Temporal Queries**: Ask "what was the state at time X?"

### Why This Framework?

This framework provides:

- **Type Safety**: Full C# type system support with minimal reflection
- **Async-First**: Complete async/await support throughout
- **Production Ready**: Error handling, logging, and observability built-in
- **Extensible**: Plugin architecture for custom repositories, handlers, and projections
- **Well-Tested**: Comprehensive examples and test scenarios
- **Zero External Dependencies**: Minimal NuGet dependencies beyond .NET libraries
- **Domain-Driven Design**: Aggregate roots, value objects, and domain events

## Architecture

### Layered Design

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│              (Controllers, API Endpoints)                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────────────┐
│                   Application Layer                          │
│  (Services, Commands, Queries, Decorators, Handlers)        │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────────────┐
│                     Domain Layer                             │
│  (Aggregates, Events, Value Objects, Exceptions)            │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────────────┐
│                 Infrastructure Layer                         │
│  (Repositories, Event Store, Cache, Event Bus)              │
└─────────────────────────────────────────────────────────────┘
```

### Directory Structure

```
Application/
├── Commands/              # CQRS Commands
│   └── CreateAccountCommand.cs
├── Queries/              # CQRS Queries
│   └── GetAccountQuery.cs
├── Handlers/             # Command & Event Handlers
│   └── EventHandlers.cs
├── Sagas/                # Saga orchestration
│   ├── ISagaHandler.cs
│   ├── ISagaRepository.cs
│   ├── InMemorySagaRepository.cs
│   └── SagaOrchestrator.cs
├── Services/             # Business Services
│   ├── IEventStore.cs
│   ├── EventStore.cs
│   ├── IEventStoreCompactionService.cs
│   ├── EventStoreCompactionService.cs
│   ├── IEventBus.cs
│   ├── EventBus.cs
│   ├── IAccountService.cs
│   ├── AccountService.cs
│   ├── IProjectionService.cs
│   ├── ProjectionService.cs
│   ├── ISnapshotService.cs
│   └── SnapshotService.cs
├── Decorators/           # Cross-Cutting Concerns
│   └── LoggingDecorator.cs
└── Extensions/           # Extension Methods
    └── CommandExtensions.cs

Domain/
├── AggregateRoots/       # DDD Aggregates
│   ├── AggregateRoot.cs
│   └── Account.cs
├── Events/               # Domain Events
│   ├── DomainEvent.cs
│   ├── EventEnvelope.cs
│   └── AccountEvents.cs
├── Sagas/                # Saga base types
│   ├── ISaga.cs
│   ├── SagaBase.cs
│   └── SagaState.cs
└── ValueObjects/         # Domain Value Objects
    ├── Money.cs
    ├── Balance.cs
    └── Transaction.cs

Data/
└── Repositories/         # Data Access
    ├── IRepository.cs
    ├── AccountRepository.cs
    ├── IEventRepository.cs
    └── InMemoryEventRepository.cs

Infrastructure/
├── Caching/              # Caching Services
├── Cli/                  # CLI command framework
│   ├── ICliCommand.cs
│   ├── CliCommandRegistry.cs
│   └── ReadModelRebuilderCommand.cs
├── Configuration/        # Infrastructure Setup
├── Decorators/           # Infrastructure Decorators
├── Events/               # Event Publishing
├── Formatters/           # Data Formatters
├── Idempotency/          # Idempotency Handling
├── Integration/          # External Integration
├── Middleware/           # HTTP Middleware
├── Observability/        # Monitoring & Metrics
├── Utilities/            # Helper Utilities
└── Workers/              # Background Workers

Presentation/
├── Controllers/          # API Controllers
│   ├── BaseApiController.cs
│   ├── AccountsController.cs
│   ├── EventsController.cs
│   ├── QueriesController.cs
│   ├── HealthController.cs
│   └── DiagnosticsController.cs
└── Models/              # Response Models

Shared/
├── Constants/            # Framework Constants
├── Enums/               # Enumerations
├── Exceptions/          # Custom Exceptions
├── Extensions/          # Extension Methods
└── Results/             # Result<T> Pattern

Configuration/
├── DatabaseConfiguration.cs
└── DependencyInjection.cs
```

### Component Responsibilities

| Component | Purpose |
|-----------|---------|
| **AggregateRoot** | Base class for domain aggregates with event sourcing |
| **Account** | Example aggregate demonstrating the pattern |
| **DomainEvent** | Base class for all domain events |
| **EventEnvelope** | Event wrapper with metadata (timestamp, version, etc.) |
| **EventStore** | Persists and retrieves event streams |
| **EventStoreCompactionService** | Prunes superseded events after snapshotting |
| **EventBus** | Publishes events to handlers |
| **ProjectionService** | Builds read models from event streams |
| **SnapshotService** | Optimizes replay performance with snapshots |
| **SagaBase** | Base class for long-running process coordination |
| **SagaOrchestrator** | Routes domain events to registered saga handlers |
| **InMemorySagaRepository** | In-memory saga state persistence |
| **CliCommandRegistry** | Dispatches CLI arguments to registered commands |
| **ReadModelRebuilderCommand** | CLI command for rebuilding read-model projections |
| **Repositories** | Data access abstraction |
| **Decorators** | Cross-cutting concerns (logging, validation) |

## Key Features

### 1. Event Sourcing
- ✅ Complete event stream persistence with versioning
- ✅ Optimistic concurrency control via version tracking
- ✅ Event metadata (timestamp, user, correlation ID)
- ✅ Checksum verification for data integrity
- ✅ Configurable event serialization

### 2. Aggregate Roots
- ✅ Event-based state management via `AggregateRoot<T>`
- ✅ Automatic version tracking and conflict detection
- ✅ Uncommitted events tracking
- ✅ Full replay capabilities for reconstructing state
- ✅ Strongly-typed command processing

### 3. Value Objects
- ✅ Immutable by design with equality semantics
- ✅ Domain-level validation (e.g., Money only allows positive amounts)
- ✅ Type-safe comparisons and operator overloading
- ✅ Serializable for event persistence

### 4. Projections
- ✅ Automated read model building from events
- ✅ Event-driven projection updates
- ✅ Projection state caching for performance
- ✅ Rebuild capabilities for model changes
- ✅ Multiple projections from same event stream

### 5. Snapshots
- ✅ Performance optimization for large aggregates
- ✅ Version-aware snapshot storage
- ✅ Intelligent snapshot retrieval
- ✅ Lifecycle management (creation, validation, cleanup)
- ✅ Configurable snapshot intervals

### 6. Event Bus
- ✅ Type-safe pub/sub messaging
- ✅ Async event handling with `async/await`
- ✅ Multiple handler support per event
- ✅ Error resilience and retry logic
- ✅ Event routing and filtering

### 7. Infrastructure
- ✅ Dependency Injection integration
- ✅ Logging throughout via `ILogger<T>`
- ✅ Caching layer for performance
- ✅ Request context preservation
- ✅ Error handling middleware
- ✅ Rate limiting support
- ✅ Idempotency key handling

### 8. Event Store Compaction
- ✅ Prune superseded events once a snapshot is in place
- ✅ Explicit version cut-off control via `CompactToVersionAsync`
- ✅ Bulk compaction across multiple aggregates with `CompactAllAsync`
- ✅ Safe: only deletes events that are fully captured by a snapshot
- ✅ Returns a `CompactionResult` with metrics (events removed, boundary version)

### 9. Saga Support
- ✅ Long-running process coordination across multiple aggregates
- ✅ `SagaBase` abstract class with lifecycle helpers (`Activate`, `Complete`, `Compensate`, `Fail`)
- ✅ `ISagaHandler<TSaga, TEvent>` strongly-typed per-event handler contract
- ✅ `InMemorySagaRepository<TSaga>` with correlation-based lookup
- ✅ `SagaOrchestrator` routes domain events to registered saga handlers
- ✅ Outbox pattern: sagas queue domain events that are published after state is persisted

### 10. Read Model Rebuilder CLI
- ✅ `rebuild-read-models` command rebuilds projections from the event store
- ✅ Supports `--aggregate <id>` for targeted single-aggregate rebuild
- ✅ Supports `--all` for a full projection rebuild
- ✅ `--dry-run` flag to preview what would be rebuilt without applying changes
- ✅ `CliCommandRegistry` is extensible – register any `ICliCommand` in DI

## RequestLogExtensions

`RequestLogExtensions` provides a set of utility methods for `RequestLog` to simplify request inspection, client identification, and correlation ID management within your application. These extensions facilitate identifying request types, resolving client IP addresses, extracting user agents, and ensuring a correlation ID is present for traceability.

### Example Usage

```csharp
using DotNetCqrsEventSourcing.Infrastructure.Models;

// Assuming 'log' is an instance of RequestLog
if (log.IsWriteOperation())
{
    var ipAddress = log.GetClientIpAddress();
    var correlationId = log.EnsureCorrelationId();
    
    Console.WriteLine($"Processing write request from {ipAddress} with Correlation ID: {correlationId}");
}

if (log.IsReadOnlyOperation())
{
    var userAgent = log.GetUserAgent() ?? "Unknown";
    Console.WriteLine($"Read-only operation from: {userAgent}");
}
```

## Installation

### Prerequisites
- .NET 10 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider (optional)

### From Source

```bash
git clone https://github.com/sarmkadan/dotnet-cqrs-eventsourcing.git
cd dotnet-cqrs-eventsourcing
dotnet restore
dotnet build
```

### Creating a New Project

```bash
dotnet new console -n MyAwesomeApp
cd MyAwesomeApp
dotnet add reference ../dotnet-cqrs-eventsourcing/dotnet-cqrs-eventsourcing.csproj
```

### Using as NuGet Package (Future)

```bash
dotnet add package Sarmkadan.CqrsEventSourcing --prerelease
```

## Quick Start

### 1. Basic Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;

// Initialize dependency injection
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

// Get the account service
var accountService = serviceProvider.GetRequiredService<IAccountService>();
```

### 2. Create an Account

```csharp
var result = await accountService.CreateAccountAsync(
    accountId: "ACC-001",
    accountHolderName: "John Doe",
    currency: "USD",
    initialBalance: 1000m
);

if (result.IsSuccess)
{
    var account = result.Data;
    Console.WriteLine($"Account created: {account.Id} with balance {account.Balance.CurrentAmount}");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}
```

### 3. Perform Transactions

```csharp
// Deposit money
var depositResult = await accountService.DepositAsync(
    accountId: "ACC-001",
    amount: 500m,
    referenceNumber: "DEP-001"
);

// Withdraw money
var withdrawResult = await accountService.WithdrawAsync(
    accountId: "ACC-001",
    amount: 200m,
    referenceNumber: "WTH-001"
);
```

### 4. Retrieve Account State

```csharp
var getResult = await accountService.GetAccountAsync("ACC-001");
if (getResult.IsSuccess)
{
    var account = getResult.Data;
    Console.WriteLine($"Current balance: {account.Balance.CurrentAmount} {account.Balance.Currency}");
    Console.WriteLine($"Transactions: {account.Transactions.Count}");
    
    foreach (var txn in account.Transactions)
    {
        Console.WriteLine($"  - {txn.Type}: {txn.Amount} ({txn.Reference})");
    }
}
```

### 5. Access Event Stream

```csharp
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var events = await eventStore.GetEventsAsync("ACC-001");

foreach (var envelope in events)
{
    Console.WriteLine($"Event: {envelope.EventType} at {envelope.Timestamp}");
}
```

## Usage Examples

For a collection of practical, runnable examples, check the `examples/` directory:

- `examples/UsageExamples/BasicUsage.cs` - Minimal setup and first call.
- `examples/UsageExamples/AdvancedUsage.cs` - Configuration, custom options, and error handling.
- `examples/UsageExamples/IntegrationExample.cs` - Wiring into .NET Dependency Injection (DI).

See the existing `examples/README.md` for details on specific scenario-based examples.

### Example 1: Complete Account Lifecycle

```csharp
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();

// Create account
var createResult = await accountService.CreateAccountAsync(
    "ACC-2024-001",
    "Alice Smith",
    "USD",
    5000m
);

var account = createResult.Data;

// Multiple transactions
await accountService.DepositAsync(account.Id, 2000m, "Salary");
await accountService.DepositAsync(account.Id, 500m, "Bonus");
await accountService.WithdrawAsync(account.Id, 1500m, "Rent Payment");

// Verify final state
var final = await accountService.GetAccountAsync(account.Id);
Console.WriteLine($"Final balance: {final.Data.Balance.CurrentAmount}");
```

### Example 2: Event Sourcing Replay

```csharp
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var events = await eventStore.GetEventsAsync("ACC-001");

// Reconstruct account at any point in time
var account = new Account();
var stateAtEvent10 = account.ReplayEvents(events.Take(10).ToList());

Console.WriteLine($"Balance after 10 events: {stateAtEvent10.Balance.CurrentAmount}");
```

### Example 3: Event Handling

```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Subscribe to account created events
await eventBus.SubscribeAsync<AccountCreated>(async (evt) =>
{
    Console.WriteLine($"Account {evt.AggregateId} created with {evt.InitialBalance}");
    // Send welcome email, create audit log, etc.
    await Task.CompletedTask;
});

// Publish event
var createdEvent = new AccountCreated(
    "ACC-001", 1, "New Account", "USD", 1000m, DateTime.UtcNow
);
await eventBus.PublishAsync(createdEvent);
```

### Example 4: Projections for Read Models

```csharp
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();

// Build projection
var projection = await projectionService.BuildProjectionAsync("ACC-001");

// Use optimized read model
Console.WriteLine($"Account Status: {projection.Status}");
Console.WriteLine($"Total Deposits: {projection.TotalDeposits}");
Console.WriteLine($"Total Withdrawals: {projection.TotalWithdrawals}");
```

### Example 5: Snapshot Usage

```csharp
var snapshotService = serviceProvider.GetRequiredService<ISnapshotService>();

// Create snapshot after processing
await snapshotService.CreateSnapshotAsync(account, "ACC-001", 100);

// Load account with snapshot
var snapshot = await snapshotService.GetSnapshotAsync("ACC-001");
if (snapshot != null)
{
    // Only replay events after snapshot
    var eventsSinceSnapshot = await eventStore.GetEventsAsync(
        "ACC-001",
        snapshot.Version
    );
}
```

### Example 6: Error Handling with Result Pattern

```csharp
var result = await accountService.WithdrawAsync("ACC-001", 10000m, "ref");

if (!result.IsSuccess)
{
    switch (result.Error)
    {
        case "InsufficientFunds":
            Console.WriteLine("Not enough balance");
            break;
        case "InvalidAmount":
            Console.WriteLine("Amount must be positive");
            break;
        default:
            Console.WriteLine($"Error: {result.Error}");
            break;
    }
}
else
{
    Console.WriteLine($"Withdrawal successful: {result.Data}");
}
```

### Example 7: Decorated Commands (Logging & Validation)

```csharp
// The framework automatically applies decorators via DI:
// 1. LoggingDecorator - logs all operations
// 2. ValidationDecorator - validates commands before execution

var result = await accountService.DepositAsync("ACC-001", 500m, "DEP-001");
// Decorator chain:
// Input -> Validation -> Logging -> Command Handler -> Logging -> Output
```

### Example 8: Testing Event Sourcing

```csharp
[TestMethod]
public async Task CreateAccount_ShouldGenerateEvent()
{
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    var accountService = serviceProvider.GetRequiredService<IAccountService>();

    await accountService.CreateAccountAsync("TEST-001", "Test User", "USD", 1000m);
    
    var events = await eventStore.GetEventsAsync("TEST-001");
    Assert.IsTrue(events.Any(e => e.EventType == nameof(AccountCreated)));
}
```

## Event Store Compaction

Compaction removes events that are no longer needed for aggregate reconstruction because they
have been captured in a snapshot.  This reduces storage costs and speeds up replays.

```csharp
// Inject the compaction service
var compactionService = serviceProvider.GetRequiredService<IEventStoreCompactionService>();
var snapshotService   = serviceProvider.GetRequiredService<ISnapshotService>();

// First create a snapshot, then compact
await snapshotService.CreateSnapshotAsync(accountId, account.Version, serializedState);
var result = await compactionService.CompactAsync(accountId);

Console.WriteLine($"Removed {result.Data!.EventsRemoved} events (kept from v{result.Data.CompactedToVersion}).");

// Or specify an explicit version boundary
var result2 = await compactionService.CompactToVersionAsync(accountId, keepFromVersion: 50);

// Bulk compaction across many aggregates (skips those without snapshots)
var bulkResult = await compactionService.CompactAllAsync(new[] { id1, id2, id3 });
```

## Saga Support

Sagas coordinate long-running processes that span multiple aggregates.  Implement
`SagaBase` to define state and react to domain events:

```csharp
// Define your saga
public class FundTransferSaga : SagaBase
{
    public override string SagaName => "FundTransferSaga";
    public string? SourceAccountId { get; private set; }
    public string? DestinationAccountId { get; private set; }

    public void OnTransferInitiated(TransferInitiatedEvent e)
    {
        SourceAccountId = e.SourceAccountId;
        DestinationAccountId = e.DestinationAccountId;
        Activate();
        // Raise a command event to debit the source account
        RaiseEvent(new DebitRequestedEvent(e.SourceAccountId, e.Amount));
    }

    public void OnDebitConfirmed(DebitConfirmedEvent e)
    {
        RaiseEvent(new CreditRequestedEvent(DestinationAccountId!, e.Amount));
    }

    public void OnCreditConfirmed(CreditConfirmedEvent _) => Complete();
    public void OnDebitFailed(DebitFailedEvent _) => Compensate();
}

// Implement a handler for each triggering event
public class FundTransferSagaHandler : ISagaHandler<FundTransferSaga, TransferInitiatedEvent>
{
    private readonly ISagaRepository<FundTransferSaga> _repo;

    public async Task<Result> HandleAsync(TransferInitiatedEvent e, CancellationToken ct = default)
    {
        var saga = new FundTransferSaga();
        saga.OnTransferInitiated(e);
        return await _repo.SaveAsync(saga, ct);
    }
}

// Register in DI
services.AddSingleton<ISagaRepository<FundTransferSaga>, InMemorySagaRepository<FundTransferSaga>>();
services.AddSingleton<FundTransferSagaHandler>();
services.AddSingleton<ISagaHandlerWrapper>(sp =>
    new SagaHandlerWrapper<FundTransferSaga, TransferInitiatedEvent>(
        sp.GetRequiredService<FundTransferSagaHandler>()));
services.AddSingleton<SagaOrchestrator>();
```

## Read Model Rebuilder CLI

Trigger a full or partial read-model rebuild from the command line:

```bash
# Rebuild projections for a single aggregate
dotnet run -- rebuild-read-models --aggregate ACC-001

# Rebuild all projections
dotnet run -- rebuild-read-models --all

# Preview without making changes
dotnet run -- rebuild-read-models --all --dry-run
```

Add custom CLI commands by implementing `ICliCommand` and registering in DI:

```csharp
services.AddSingleton<ICliCommand, MyCustomCommand>();
services.AddSingleton<CliCommandRegistry>();
```

## API Reference

### IAccountService

```csharp
public interface IAccountService
{
    Task<Result<Account>> CreateAccountAsync(
        string accountId,
        string accountHolderName,
        string currency,
        decimal initialBalance,
        CancellationToken cancellationToken = default);

    Task<Result<decimal>> DepositAsync(
        string accountId,
        decimal amount,
        string referenceNumber,
        CancellationToken cancellationToken = default);

    Task<Result<decimal>> WithdrawAsync(
        string accountId,
        decimal amount,
        string referenceNumber,
        CancellationToken cancellationToken = default);

    Task<Result<Account>> GetAccountAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> CloseAccountAsync(
        string accountId,
        string reason,
        CancellationToken cancellationToken = default);
}
```

### IEventStore

```csharp
public interface IEventStore
{
    Task SaveEventsAsync(
        string aggregateId,
        int expectedVersion,
        IEnumerable<DomainEvent> uncommittedEvents,
        CancellationToken cancellationToken = default);

    Task<List<EventEnvelope>> GetEventsAsync(
        string aggregateId,
        int fromVersion = 0,
        CancellationToken cancellationToken = default);

    Task<EventEnvelope?> GetEventAsync(
        string aggregateId,
        int version,
        CancellationToken cancellationToken = default);

    Task<List<EventEnvelope>> GetAllEventsAsync(
        CancellationToken cancellationToken = default);
}
```

### IEventBus

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) 
        where T : IDomainEvent;

    Task SubscribeAsync<T>(
        Func<T, Task> handler,
        CancellationToken cancellationToken = default) 
        where T : IDomainEvent;

    Task UnsubscribeAsync<T>(CancellationToken cancellationToken = default) 
        where T : IDomainEvent;
}
```

### IProjectionService

```csharp
public interface IProjectionService
{
    Task<AccountProjection> BuildProjectionAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    Task RebuildProjectionsAsync(
        CancellationToken cancellationToken = default);

    Task ClearProjectionAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
```

### ISnapshotService

```csharp
public interface ISnapshotService
{
    Task CreateSnapshotAsync(
        Account aggregate,
        string aggregateId,
        int version,
        CancellationToken cancellationToken = default);

    Task<AggregateSnapshot?> GetSnapshotAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);

    Task DeleteSnapshotAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);
}
```

## Configuration

The framework uses the standard .NET `IOptions` pattern with comprehensive validation. Configure settings via `appsettings.json` or other IConfiguration sources (environment variables, command-line arguments, etc.).

### Quick Start

Create an `appsettings.json` file in your application's root directory:

```json
{
  "DotnetCqrsEventsourcing": {
    "EventStoreConnectionString": "Server=localhost;Database=EventStore;User Id=sa;Password=YourStrong!Passw0rd;",
    "ProjectionStoreConnectionString": "Server=localhost;Database=ProjectionStore;User Id=sa;Password=YourStrong!Passw0rd;",
    "SnapshotStoreConnectionString": "Server=localhost;Database=SnapshotStore;User Id=sa;Password=YourStrong!Passw0rd;"
  }
}
```

### Complete Configuration Example

```json
{
  "DotnetCqrsEventsourcing": {
    "EventStoreConnectionString": "Server=sql-prod.database.windows.net;Database=EventStore;User Id=app-user;Password=ComplexPassword123!;",
    "ProjectionStoreConnectionString": "Server=sql-prod.database.windows.net;Database=ProjectionStore;User Id=app-user;Password=ComplexPassword123!;",
    "SnapshotStoreConnectionString": "Server=sql-prod.database.windows.net;Database=SnapshotStore;User Id=app-user;Password=ComplexPassword123!;",
    "MaxEventsCached": 20000,
    "CacheExpirationSeconds": 7200,
    "EnableEventCompression": true,
    "BatchWriteSize": 200,
    "ParallelReaderCount": 8,
    "AutoCreateSnapshots": true,
    "SnapshotFrequency": 25,
    "MinVersionForSnapshot": 50,
    "VerifyEventChecksums": true,
    "RetentionPolicy": 2,
    "RetentionDays": 90
  }
}
```

### Settings Reference

All settings support environment variable overrides using the format:
`DotnetCqrsEventsourcing__<SettingName>` (double underscore separator).

| Setting | Type | Default | Environment Variable | Description |
| :--- | :--- | :--- | :--- | :--- |
| `EventStoreConnectionString` | string | - | `DotnetCqrsEventsourcing__EventStoreConnectionString` | **Required**. Connection string for the event store database. This is where all domain events are persisted. Minimum length: 10 characters. |
| `ProjectionStoreConnectionString` | string | - | `DotnetCqrsEventsourcing__ProjectionStoreConnectionString` | **Required**. Connection string for the projection store database. This is where read models are stored for query optimization. Minimum length: 10 characters. |
| `SnapshotStoreConnectionString` | string | - | `DotnetCqrsEventsourcing__SnapshotStoreConnectionString` | **Required**. Connection string for the snapshot store database. This is where aggregate snapshots are persisted to optimize replay performance. Minimum length: 10 characters. |
| `MaxEventsCached` | int | 10,000 | `DotnetCqrsEventsourcing__MaxEventsCached` | Maximum number of events to keep in memory cache. Higher values improve performance for frequently accessed aggregates but increase memory usage. Range: 1-1,000,000. |
| `CacheExpirationSeconds` | int | 3,600 | `DotnetCqrsEventsourcing__CacheExpirationSeconds` | Maximum age of cached events in seconds. Events older than this will be evicted from cache. Set to 0 to disable caching. Range: 0-86,400 (24 hours). |
| `EnableEventCompression` | bool | false | `DotnetCqrsEventsourcing__EnableEventCompression` | Enable event compression for large events. When enabled, events are compressed before storage to reduce database size. |
| `BatchWriteSize` | int | 100 | `DotnetCqrsEventsourcing__BatchWriteSize` | Batch size for bulk event writes. Larger batches improve write performance but increase memory usage during writes. Range: 1-10,000. |
| `ParallelReaderCount` | int | ProcessorCount | `DotnetCqrsEventsourcing__ParallelReaderCount` | Number of parallel event reader threads. Controls how many events can be read concurrently for better throughput. Range: 1-64. |
| `AutoCreateSnapshots` | bool | true | `DotnetCqrsEventsourcing__AutoCreateSnapshots` | Automatically create snapshots when `SnapshotFrequency` threshold is reached. When false, snapshots must be created manually. |
| `SnapshotFrequency` | int | 50 | `DotnetCqrsEventsourcing__SnapshotFrequency` | Frequency of automatic snapshots (number of events). After this many events, a snapshot will be automatically created if `AutoCreateSnapshots` is true. Range: 1-1,000. |
| `MinVersionForSnapshot` | long | 10 | `DotnetCqrsEventsourcing__MinVersionForSnapshot` | Minimum version before creating snapshots. Snapshots will only be created for aggregates that have reached this version. Range: 0-1,000,000. |
| `VerifyEventChecksums` | bool | true | `DotnetCqrsEventsourcing__VerifyEventChecksums` | Verify event checksums on read. When enabled, validates event integrity to detect data corruption. Disable only for performance testing. |
| `RetentionPolicy` | enum | 0 (Infinite) | `DotnetCqrsEventsourcing__RetentionPolicy` | Retention policy for old events. Options: `0` (Infinite - keep all events), `1` (Limited - keep for specified days), `2` (Snapshots - keep only snapshots and recent events), `3` (Archive - move old events to cold storage). |
| `RetentionDays` | int | 365 | `DotnetCqrsEventsourcing__RetentionDays` | Days to retain events when `RetentionPolicy` is set to `Limited`. Events older than this will be automatically removed. Range: 1-3,650 (10 years). |

### Environment Variables

All settings can also be configured via environment variables:

```bash
# Example environment variables
export DotnetCqrsEventsourcing__EventStoreConnectionString="Server=localhost;Database=EventStore;..."
export DotnetCqrsEventsourcing__MaxEventsCached=5000
export DotnetCqrsEventsourcing__SnapshotFrequency=100
```

### Validation

The framework automatically validates all configuration settings on startup:

- **Required fields** (`EventStoreConnectionString`, `ProjectionStoreConnectionString`, `SnapshotStoreConnectionString`) must be provided
- **Range validations** ensure numeric values are within acceptable ranges
- **MinLength validations** ensure connection strings are not empty
- **Validation errors** throw exceptions immediately at application startup, preventing runtime failures

### Registration

To register and validate options in your DI container:

```csharp
// In your Startup or Program.cs
services.AddCqrsFramework(configuration);
```

The framework automatically:
- Binds configuration to `DotnetCqrsEventsourcingOptions`
- Validates all settings using DataAnnotations
- Throws exceptions for invalid configuration on startup
- Makes options available via `IOptions<DotnetCqrsEventsourcingOptions>`

### Accessing Options

```csharp
// Constructor injection
public class MyService
{
    private readonly DotnetCqrsEventsourcingOptions _options;
    
    public MyService(IOptions<DotnetCqrsEventsourcingOptions> options)
    {
        _options = options.Value;
        
        // Access configuration values
        var maxEvents = _options.MaxEventsCached;
        var connectionString = _options.EventStoreConnectionString;
    }
}

// Or directly from DI
var options = serviceProvider.GetRequiredService<IOptions<DotnetCqrsEventsourcingOptions>>();
var snapshotFrequency = options.Value.SnapshotFrequency;
```

### Production Configuration Examples

#### Development Configuration

```json
{
  "DotnetCqrsEventsourcing": {
    "EventStoreConnectionString": "Server=localhost;Database=EventStoreDev;User Id=sa;Password=YourStrong!Passw0rd;",
    "ProjectionStoreConnectionString": "Server=localhost;Database=ProjectionStoreDev;User Id=sa;Password=YourStrong!Passw0rd;",
    "SnapshotStoreConnectionString": "Server=localhost;Database=SnapshotStoreDev;User Id=sa;Password=YourStrong!Passw0rd;",
    "MaxEventsCached": 5000,
    "CacheExpirationSeconds": 1800,
    "BatchWriteSize": 50,
    "ParallelReaderCount": 2,
    "AutoCreateSnapshots": true,
    "SnapshotFrequency": 100,
    "MinVersionForSnapshot": 20,
    "VerifyEventChecksums": true,
    "RetentionPolicy": 0
  }
}
```

#### Production Configuration

```json
{
  "DotnetCqrsEventsourcing": {
    "EventStoreConnectionString": "Server=sql-prod.database.windows.net;Database=EventStore;User Id=app-user;Password=ComplexPassword123!;",
    "ProjectionStoreConnectionString": "Server=sql-prod.database.windows.net;Database=ProjectionStore;User Id=app-user;Password=ComplexPassword123!;",
    "SnapshotStoreConnectionString": "Server=sql-prod.database.windows.net;Database=SnapshotStore;User Id=app-user;Password=ComplexPassword123!;",
    "MaxEventsCached": 20000,
    "CacheExpirationSeconds": 7200,
    "EnableEventCompression": true,
    "BatchWriteSize": 200,
    "ParallelReaderCount": 8,
    "AutoCreateSnapshots": true,
    "SnapshotFrequency": 25,
    "MinVersionForSnapshot": 50,
    "VerifyEventChecksums": true,
    "RetentionPolicy": 2,
    "RetentionDays": 90
  }
}
```

### Configuration Best Practices

1. **Connection Strings**: Use managed identities or secret management for production credentials
2. **Caching**: Adjust `MaxEventsCached` based on your aggregate size and access patterns
3. **Snapshots**: Configure `SnapshotFrequency` based on your aggregate complexity (more complex = more frequent snapshots)
4. **Retention**: Set appropriate retention policies for compliance and cost management
5. **Validation**: Always validate configuration in your CI/CD pipeline before deployment


### Troubleshooting Configuration Issues


**Error: Required configuration is missing**
- Ensure all three connection strings are provided in your configuration
- Verify the configuration section name is correct: `DotnetCqrsEventsourcing`


**Error: Validation failed**
- Check that numeric values are within valid ranges
- Ensure connection strings are not empty strings
- For `RetentionDays`, ensure it's >= 0

**Error: Configuration not applied**
- Verify you're calling `services.AddCqrsFramework(configuration)`
- Ensure your configuration source (appsettings.json, environment variables, etc.) is properly loaded
- Check that the configuration section name matches exactly

## Deployment

### Docker Deployment

See `Dockerfile` and `docker-compose.yml` for containerization.

```bash
docker build -t dotnet-cqrs:latest .
docker run -p 5000:8080 dotnet-cqrs:latest
```

## Docker Usage

This project includes a full Docker setup for development and production.

### Running with Docker Compose

To start the API, SQL Server, Redis, and Adminer services:

```bash
docker-compose up -d
```

The services will be available at:
- **API**: http://localhost:8080
- **Adminer**: http://localhost:8081

### Build and Run API Only

To build the API image:
```bash
docker build -t dotnet-cqrs:latest .
```

To run the API container:
```bash
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production dotnet-cqrs:latest
```

### Kubernetes Deployment

Basic Kubernetes YAML available in deployment guides.

### Configuration for Production

1. **Event Store**: Replace in-memory store with SQL/NoSQL implementation
2. **Event Bus**: Integrate with RabbitMQ, Service Bus, or Kafka
3. **Caching**: Add Redis for projection caching
4. **Monitoring**: Integrate with Application Insights or Datadog
5. **Persistence**: Implement database-backed repositories

## Testing

Run the full test suite:

```bash
dotnet test
```

Run with coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

The test suite covers:

- **Unit Tests**: Domain aggregate behaviour, value object semantics, guard clauses
- **Integration Tests**: Event store read/write, projection building, snapshot lifecycle
- **Concurrency Tests**: Optimistic locking and version-conflict scenarios

All PRs must maintain or improve test coverage.

## Performance

Performance benchmarks are available in the `dotnet-cqrs-eventsourcing.Benchmarks` project using [BenchmarkDotNet](https://benchmarkdotnet.org/).


### Running Benchmarks

```bash
cd dotnet-cqrs-eventsourcing.Benchmarks
dotnet run -c Release
```

### Latest Benchmark Results

Benchmarks measured on .NET 10 (Intel Core i7, in-memory event store):


| Benchmark Category | Operation | Mean | Error | StdDev | Allocated |
|------------------|-----------|------|-------|--------|-----------|
| **EventStore** | Event append (single) | 12.3 μs | 0.2 μs | 0.2 μs | 1.2 KB |
| **EventStore** | Event append (batch 100) | 1.4 ms | 0.03 ms | 0.03 ms | 12.8 KB |
| **AggregateRoot** | Replay 100 events | 48.7 μs | 0.9 μs | 1.1 μs | 4.5 KB |
| **AggregateRoot** | Replay 1,000 events | 472 μs | 9.2 μs | 11.1 μs | 44.8 KB |
| **AggregateRoot** | Replay 10,000 events | 4.6 ms | 0.09 ms | 0.11 ms | 448.2 KB |
| **AccountService** | Create account | 15.8 μs | 0.3 μs | 0.3 μs | 2.1 KB |
| **AccountService** | Complete lifecycle (100 txns) | 1.8 ms | 0.04 ms | 0.04 ms | 156.4 KB |

### Key Characteristics

- **Throughput**: Event store can process ~80,000 events/sec for single events
- **Latency**: Aggregate replay scales linearly with event count
- **Memory**: Minimal allocations for hot paths (hot path = < 5 KB per operation)
- **Snapshots**: Can reduce replay time by up to **90%** for aggregates with > 500 events
- **Optimistic Concurrency**: Version checks add < 1 μs overhead

### Performance Optimization Tips

1. **Use snapshots** for aggregates with > 500 events to reduce replay time
2. **Batch events** when possible (e.g., 100 events/batch = ~700 μs vs 12.3 μs/event)
3. **Cache projections** for read-heavy workloads (cached reads: < 1 ms)
4. **Enable MemoryDiagnoser** to identify allocation hotspots in your code

For detailed benchmark results and to run your own tests, see the [Benchmarks README](dotnet-cqrs-eventsourcing.Benchmarks/README.md).

## Troubleshooting

### Common Issues

**Q: OptimisticConcurrencyException when saving events**
A: Version mismatch detected. Two writes attempted same aggregate simultaneously.
```csharp
// Solution: Implement retry logic
int maxRetries = 3;
for (int i = 0; i < maxRetries; i++)
{
    try
    {
        await eventStore.SaveEventsAsync(aggregateId, expectedVersion, events);
        break;
    }
    catch (OptimisticConcurrencyException)
    {
        if (i == maxRetries - 1) throw;
        await Task.Delay(100 * (i + 1)); // Exponential backoff
    }
}
```

**Q: Events not being published to handlers**
A: Verify handlers are subscribed before events are published.
```csharp
// Subscribe first
await eventBus.SubscribeAsync<AccountCreated>(HandleAccountCreated);

// Then publish
await eventBus.PublishAsync(createdEvent);
```

**Q: Projection out of sync with events**
A: Rebuild projections to restore consistency.
```csharp
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();
await projectionService.RebuildProjectionsAsync();
```

**Q: Poor performance with large event streams**
A: Use snapshots to reduce replay time.
```csharp
// Save snapshot every N events
if (eventCount % 100 == 0)
{
    await snapshotService.CreateSnapshotAsync(account, accountId, eventCount);
}
```

### Debug Logging

Enable detailed logging:

```csharp
services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Debug);
});
```

## Related Projects

- [dotnet-event-bus](https://github.com/sarmkadan/dotnet-event-bus) - In-process and distributed event bus for .NET - pub/sub, request/reply, dead letter, polymorphic handlers
- [dotnet-outbox-pattern](https://github.com/sarmkadan/dotnet-outbox-pattern) - Transactional outbox pattern for .NET - guaranteed message delivery, deduplication, ordering, dead letter handling

### Integration Examples

**Forward domain events to `dotnet-event-bus` for distributed publishing:**

```csharp
// Bridge the CQRS IEventBus to a distributed bus at composition root
services.AddSingleton<IEventBus>(sp =>
{
    var distributed = sp.GetRequiredService<IDistributedEventBus>();
    var local = new EventBus(sp.GetRequiredService<ILogger<EventBus>>());
    local.OnPublish += async evt => await distributed.PublishAsync(evt);
    return local;
});
```

**Relay committed events through `dotnet-outbox-pattern` for guaranteed delivery:**

```csharp
// After persisting events, write them to the outbox atomically
await eventStore.SaveEventsAsync(aggregateId, expectedVersion, uncommittedEvents);
foreach (var evt in uncommittedEvents)
{
    await outboxWriter.WriteAsync(evt.GetType().Name, JsonSerializer.Serialize(evt));
}
// The outbox worker delivers events reliably, surviving process restarts
```

## Contributing

### How to Contribute

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Code Style

- Follow C# naming conventions (PascalCase for public members)
- Use `async/await` for I/O operations
- Add XML documentation comments to public APIs
- Write unit tests for new features

## License

This project is licensed under the [MIT License](LICENSE).

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)


