# dotnet-cqrs-eventsourcing

Production CQRS + Event Sourcing framework for .NET - aggregate roots, projections, snapshots, replay

## Overview

A complete, production-grade implementation of CQRS (Command Query Responsibility Segregation) and Event Sourcing patterns for .NET 10. This framework provides:

- **Event Sourcing**: Complete event stream persistence and replay
- **Aggregate Roots**: DDD-based aggregate management with event sourcing
- **CQRS**: Command and query separation at the domain level
- **Projections**: Read model building from event streams
- **Snapshots**: Performance optimization for large aggregates
- **Event Bus**: In-process pub/sub for event handling
- **Type-Safe**: Full C# type safety with minimal reflection
- **Async/Await**: Full async support throughout

## Architecture

### Layers

```
Application/
├── Services/               # Business logic & CQRS operations
│   ├── IEventStore        # Event persistence interface
│   ├── EventStore         # Event store implementation
│   ├── IEventBus          # Event publishing interface
│   ├── EventBus           # Event bus implementation
│   ├── IAccountService    # Business operations
│   ├── AccountService     # Account operations implementation
│   ├── IProjectionService # Read model building
│   ├── ProjectionService  # Projection implementation
│   ├── ISnapshotService   # Snapshot management
│   └── SnapshotService    # Snapshot implementation

Domain/
├── AggregateRoots/        # DDD aggregates
│   ├── AggregateRoot      # Base aggregate class
│   └── Account            # Account aggregate
├── Events/                # Domain events
│   ├── DomainEvent        # Base event class
│   ├── EventEnvelope      # Event wrapper with metadata
│   └── AccountEvents      # Concrete events
└── ValueObjects/          # Domain value objects
    ├── Money              # Monetary amount
    ├── Balance            # Account balance
    └── Transaction        # Transaction record

Data/
└── Repositories/          # Data access layer
    ├── IRepository<T>     # Generic repository
    ├── AccountRepository  # Account repository
    ├── IEventRepository   # Event store repository
    └── InMemoryEventRepository # In-memory event store

Shared/
├── Exceptions/            # Domain & CQRS exceptions
├── Enums/                 # Domain enumerations
├── Constants/             # Framework constants
└── Results/               # Result<T> pattern
```

## Features

### 1. Event Sourcing
- Complete event stream persistence
- Optimistic concurrency control
- Event versioning and metadata
- Checksum verification

### 2. Aggregate Roots
- Event-based state management
- Automatic version tracking
- Event uncommitted tracking
- Full replay support

### 3. Value Objects
- Immutable design
- Domain validation
- Operator overloading
- Type-safe comparisons

### 4. Projections
- Automated read model building
- Event-driven updates
- Projection state caching
- Rebuild capabilities

### 5. Snapshots
- Performance optimization
- Version-based snapshots
- Snapshot retrieval
- Lifecycle management

### 6. Event Bus
- Type-safe pub/sub
- Async event handling
- Multi-handler support
- Error resilience

## Quick Start

### Installation

```bash
dotnet new console -n MyCqrsApp
cd MyCqrsApp
# Add project reference to dotnet-cqrs-eventsourcing
```

### Usage Example

```csharp
// Setup DI
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

// Get services
var accountService = serviceProvider.GetRequiredService<IAccountService>();

// Create account
var result = await accountService.CreateAccountAsync(
    "ACC-001",
    "John Doe",
    "USD",
    1000m
);

if (result.IsSuccess)
{
    var account = result.Data;
    
    // Deposit funds
    await accountService.DepositAsync(account.Id, 500m, "Deposit ref");
    
    // Withdraw funds
    await accountService.WithdrawAsync(account.Id, 200m, "Withdrawal ref");
    
    // Get updated account
    var updated = await accountService.GetAccountAsync(account.Id);
}
```

## Domain Model

### Account Aggregate

The framework includes a complete Account aggregate demonstrating all patterns:

```csharp
// Create and manage accounts
account.CreateAccount("ACC-001", "John Doe", "USD", 1000m);

// Deposit and withdraw
account.Deposit(500m, "deposit-001");
account.Withdraw(200m, "withdrawal-001");

// Check balance
var balance = account.Balance.CurrentAmount; // 1300 USD

// View transactions
var transactions = account.Transactions;

// Close account
account.CloseAccount("Account closure");
```

## Event Types

- `AccountCreated`: When account is opened
- `MoneyDeposited`: When funds are added
- `MoneyWithdrawn`: When funds are removed
- `BalanceUpdated`: When balance changes
- `AccountClosed`: When account is closed

## Value Objects

- **Money**: Currency-aware amount with validation
- **Balance**: Account balance with hold/available tracking
- **Transaction**: Individual transaction record

## Exceptions

- **DomainException**: Domain-level errors
- **CqrsException**: CQRS infrastructure errors
- **AggregateNotFoundException**: Missing aggregate
- **EventStreamException**: Event persistence issues

## Configuration

Configure framework in DI:

```csharp
services.AddCqrsFramework();
serviceProvider.ConfigureEventHandlers();
```

## Testing

Run the included demo:

```bash
dotnet run
```

This demonstrates:
1. Creating accounts
2. Deposits and withdrawals
3. Event stream retrieval
4. Projection building
5. Snapshot creation
6. Account closure

## Performance

- **In-Memory Store**: Suitable for testing and small-scale apps
- **Optimistic Concurrency**: Version-based conflict detection
- **Snapshots**: Reduce replay time for large event streams
- **Projections**: Fast read model access

## Extension Points

Extend the framework by:

1. **Custom Aggregates**: Inherit from `AggregateRoot`
2. **Custom Events**: Inherit from `DomainEvent`
3. **Custom Repositories**: Implement `IRepository<T>`, `IEventRepository`
4. **Custom Projections**: Implement projection logic in `ProjectionService`
5. **Event Handlers**: Subscribe via `IEventBus`

## License

MIT - See LICENSE file

## Author

Vladyslav Zaiets  
CTO & Software Architect  
https://sarmkadan.com
