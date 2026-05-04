# Architecture Guide

Deep dive into the CQRS + Event Sourcing architecture and design patterns.

## Core Concepts

### Domain-Driven Design (DDD)

The framework is built on DDD principles:

- **Aggregate Roots**: Entities that define consistency boundaries
- **Value Objects**: Immutable objects representing domain concepts
- **Domain Events**: Facts about things that happened in the domain
- **Repositories**: Abstraction for persistence

### Event Sourcing

Instead of storing current state, we store the sequence of events that led to that state.

```
Traditional: [State] -> Mutate -> [New State] (Old state lost)
Event Sourcing: [Event1] -> [Event2] -> [Event3] -> Current State (Full history kept)
```

Benefits:
- Complete audit trail
- Time travel capability
- Event-driven architecture
- Compliance-friendly

### CQRS Pattern

Separates read and write models:

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐
│   Commands  │────>│  Write Model │────>│  Event Store │
│             │     │  (Aggregates)│     │              │
└─────────────┘     └──────────────┘     └──────────────┘
                                                │
                                                │ Events
                                                v
                                        ┌──────────────┐
                                        │ Event Handlers
                                        └──────────────┘
                                                │
                                                v
                                        ┌──────────────┐
                                        │  Read Models │
                                        │(Projections) │
                                        └──────────────┘
                                                │
┌─────────────┐     ┌──────────────┐     ┌─────────────┘
│   Queries   │────>│  Read Model  │<────┘
│             │     │   (Cache)    │
└─────────────┘     └──────────────┘
```

## Architectural Layers

### Domain Layer

Core business logic, not dependent on infrastructure.

```
AggregateRoot<T>          - Base class for aggregates
  ├── Apply(DomainEvent)  - Apply event to state
  ├── Raise(DomainEvent)  - Record uncommitted event
  └── Replay(events)      - Reconstruct state from events

DomainEvent               - Base class for all events
ValueObject               - Immutable domain concepts
DomainException           - Domain-level errors
```

Example:
```csharp
public class Account : AggregateRoot
{
    public string AccountId { get; private set; }
    public Balance Balance { get; private set; }
    public List<Transaction> Transactions { get; private set; }
    
    // Domain logic determines what events to raise
    public void Deposit(decimal amount, string reference)
    {
        // Validate using domain logic
        if (amount <= 0)
            throw new DomainException("Amount must be positive");
        
        // Raise event (uncommitted)
        RaiseEvent(new MoneyDeposited(
            Id, 
            Version + 1,
            amount,
            reference,
            DateTime.UtcNow
        ));
    }
    
    // Apply event to state
    protected override void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case MoneyDeposited deposited:
                Balance = Balance.Add(deposited.Amount);
                Transactions.Add(new Transaction(deposited.Amount, "Deposit", deposited.Reference));
                break;
        }
    }
}
```

### Application Layer

Orchestrates domain logic and coordinates services.

```
Command Handler           - Handles commands from clients
Query Handler            - Handles queries from clients
Event Handler            - Responds to domain events
Service                  - Orchestrates aggregates and repositories
Decorator                - Cross-cutting concerns (logging, validation)
```

Example flow:
```
Client -> Command -> CommandHandler -> AccountService -> Account.Deposit() 
  -> DomainEvent -> EventStore -> EventBus -> EventHandlers -> Projections
```

Key services:

1. **AccountService**: Manages account aggregate operations
2. **EventStore**: Persists and retrieves events
3. **EventBus**: Publishes events to handlers
4. **ProjectionService**: Builds read models
5. **SnapshotService**: Optimizes replay performance

### Infrastructure Layer

Technical implementation details.

```
EventRepository          - Event persistence
CacheService             - Caching layer
HttpClientFactory        - HTTP integration
EventDispatcher          - Event publishing
Middleware               - HTTP pipeline customization
Decorators               - Logging, validation, idempotency
```

### Data Layer

Data access abstraction.

```
IRepository<T>           - Generic CRUD interface
IEventRepository         - Event stream interface
AccountRepository        - Account-specific repository
InMemoryEventRepository  - In-memory implementation
```

## Event Sourcing Deep Dive

### Event Stream

An ordered sequence of events for an aggregate:

```
AggregateId: ACC-001
Version: 1 | Event: AccountCreated | Data: { Name: "John", Currency: "USD", Balance: 1000 }
Version: 2 | Event: MoneyDeposited | Data: { Amount: 500, Reference: "DEP-001" }
Version: 3 | Event: MoneyWithdrawn | Data: { Amount: 200, Reference: "WTH-001" }
...
```

### Storing Events

```csharp
public async Task SaveEventsAsync(
    string aggregateId,
    int expectedVersion,
    IEnumerable<DomainEvent> events)
{
    // Check optimistic concurrency
    var currentVersion = await GetCurrentVersionAsync(aggregateId);
    if (currentVersion != expectedVersion)
        throw new OptimisticConcurrencyException();
    
    // Persist events with new versions
    foreach (var @event in events)
    {
        var envelope = new EventEnvelope
        {
            AggregateId = aggregateId,
            Version = ++currentVersion,
            EventType = @event.GetType().Name,
            Data = JsonConvert.SerializeObject(@event),
            Timestamp = DateTime.UtcNow
        };
        
        await _repository.InsertAsync(envelope);
    }
}
```

### Replaying Events

Reconstruct state by applying events in sequence:

```csharp
public Account ReplayEvents(List<EventEnvelope> events)
{
    var account = new Account();
    
    foreach (var envelope in events)
    {
        var @event = JsonConvert.DeserializeObject(
            envelope.Data,
            Type.GetType(envelope.EventType)
        ) as DomainEvent;
        
        account.Apply(@event);
    }
    
    return account;
}
```

## Snapshot Architecture

Snapshots optimize performance by reducing replay distance:

```
Events:     E1 -> E2 -> E3 -> E4 -> E5 -> E6 -> E7 -> E8
            ├─────────────────┤
            Snapshot at E3

Load:       Snapshot(State@E3) -> Replay[E4, E5, E6, E7, E8]
            Instead of:        Replay[E1, E2, E3, E4, E5, E6, E7, E8]
```

Implementation:
```csharp
public async Task<Account> LoadAccountAsync(string accountId)
{
    // Try to load snapshot
    var snapshot = await _snapshotService.GetSnapshotAsync(accountId);
    
    Account account;
    int fromVersion = 0;
    
    if (snapshot != null)
    {
        // Restore from snapshot
        account = snapshot.RestoreAggregate();
        fromVersion = snapshot.Version;
    }
    else
    {
        // Start fresh
        account = new Account();
    }
    
    // Replay events after snapshot
    var events = await _eventStore.GetEventsAsync(accountId, fromVersion);
    account.ReplayEvents(events);
    
    return account;
}
```

## Projection Architecture

Read models optimized for queries:

```
Events -> Event Bus -> Handlers -> Update Projection -> Query

Example:
MoneyDeposited -> AccountDepositHandler -> Update AccountSummary -> Get balance by account ID
```

Projection structure:
```csharp
public class AccountProjection
{
    public string AccountId { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public int TransactionCount { get; set; }
    public AccountStatus Status { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

Projection rebuild:
```
1. Clear existing projections
2. Get all events from event store
3. Replay through projection handlers
4. Rebuild complete read models
5. Update caches
```

## Concurrency Control

### Optimistic Concurrency

Uses version numbers to detect conflicts:

```csharp
// Thread 1
var account = await LoadAccountAsync("ACC-001");  // Version: 3
account.Deposit(100);
// Before saving: currentVersion = 3

// Thread 2 (concurrent)
var account2 = await LoadAccountAsync("ACC-001"); // Version: 3
account2.Withdraw(50);
await SaveAsync(account2, expectedVersion: 3);    // Success, now Version: 4

// Thread 1 (resume)
await SaveAsync(account, expectedVersion: 3);     // FAILS - current is 4, expected 3
```

Retry pattern:
```csharp
public async Task SaveWithRetryAsync(Account account, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await _eventStore.SaveEventsAsync(
                account.Id,
                account.Version,
                account.GetUncommittedEvents()
            );
            return;
        }
        catch (OptimisticConcurrencyException)
        {
            if (i == maxRetries - 1) throw;
            
            // Reload and reapply
            account = await LoadAccountAsync(account.Id);
            await Task.Delay(100 * (i + 1));
        }
    }
}
```

## Error Handling

### Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T Data { get; set; }
    public string Error { get; set; }
}
```

Usage:
```csharp
var result = await accountService.WithdrawAsync("ACC-001", 5000m, "ref");

if (!result.IsSuccess)
{
    switch (result.Error)
    {
        case "InsufficientFunds":
            // Handle insufficient funds
            break;
        case "InvalidAmount":
            // Handle invalid amount
            break;
    }
}
```

### Exception Hierarchy

```
Exception
├── DomainException
│   ├── InvalidAccountException
│   ├── InsufficientFundsException
│   └── InvalidMoneyException
├── CqrsException
│   ├── OptimisticConcurrencyException
│   ├── EventStreamException
│   └── AggregateNotFoundException
└── Infrastructure exceptions
```

## Decorators & Cross-Cutting Concerns

### Decorator Pattern

Adds functionality without modifying original code:

```
Command -> LoggingDecorator -> ValidationDecorator -> Handler

Execution order:
1. LoggingDecorator.Before()
2. ValidationDecorator.Before()
3. CommandHandler.Execute()
4. ValidationDecorator.After()
5. LoggingDecorator.After()
```

Example:
```csharp
public class LoggingDecorator<T> : ICommandHandler<T> where T : ICommand
{
    public async Task<Result> HandleAsync(T command)
    {
        _logger.LogInformation($"Executing: {typeof(T).Name}");
        
        var stopwatch = Stopwatch.StartNew();
        var result = await _next.HandleAsync(command);
        stopwatch.Stop();
        
        _logger.LogInformation($"Completed: {stopwatch.ElapsedMilliseconds}ms");
        
        return result;
    }
}
```

## Message Bus Integration

Event-driven architecture:

```
Aggregate -> Domain Event -> Event Bus -> Handlers

Types of handlers:
1. Projection builders (update read models)
2. Saga coordinators (orchestrate workflows)
3. External system integrators (webhooks, APIs)
4. Notification services (email, SMS)
5. Analytics (event metrics)
```

Example handler:
```csharp
public class AccountCreatedHandler : IEventHandler<AccountCreated>
{
    public async Task HandleAsync(AccountCreated @event)
    {
        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(@event.AccountHolderId);
        
        // Update projection
        await _projectionService.UpdateAccountProjectionAsync(@event);
        
        // Record audit
        await _auditService.LogAsync("account_created", @event.AggregateId);
    }
}
```

## Performance Considerations

### Caching Strategy

```
Query -> Cache (L1) -> Event Store (L2)
         
1. Check memory cache
2. If miss, load from event store
3. Cache result for future queries
4. Invalidate on event
```

### Event Stream Pagination

Load events in batches:
```csharp
var pageSize = 100;
var page = 0;

while (true)
{
    var events = await _eventStore.GetEventsAsync(
        aggregateId,
        offset: page * pageSize,
        limit: pageSize
    );
    
    if (events.Count == 0) break;
    
    ProcessEvents(events);
    page++;
}
```

### Snapshot Strategy

Create snapshots at regular intervals:
```
EventCount % SnapshotInterval == 0 -> Create Snapshot
Replay: Max events = SnapshotInterval
```

## Testing Architecture

### Unit Testing

Test aggregates in isolation:
```csharp
[TestMethod]
public void Deposit_IncreasesBalance()
{
    // Arrange
    var account = new Account();
    account.Apply(new AccountCreated("ACC-001", 1, "John", "USD", 1000m, DateTime.UtcNow));
    
    // Act
    account.Deposit(500m, "DEP-001");
    
    // Assert
    Assert.AreEqual(1500m, account.Balance.CurrentAmount);
}
```

### Integration Testing

Test with real event store:
```csharp
[TestMethod]
public async Task CreateAccount_PersistsEvents()
{
    // Arrange
    var eventStore = new InMemoryEventRepository();
    var service = new AccountService(eventStore);
    
    // Act
    var result = await service.CreateAccountAsync("ACC-001", "John", "USD", 1000m);
    
    // Assert
    var events = await eventStore.GetEventsAsync("ACC-001");
    Assert.IsTrue(events.Any(e => e.EventType == "AccountCreated"));
}
```

## Scaling Considerations

### Horizontal Scaling

- **Read Model Cache**: Use Redis for distributed caching
- **Event Bus**: Replace in-process with RabbitMQ/Kafka
- **Event Store**: Use SQL database or DocumentDB
- **Multiple Instances**: Load balance across servers

### Event Store Optimization

```csharp
// Batch events for better performance
public async Task SaveEventsInBatchAsync(
    List<(string AggregateId, List<DomainEvent> Events)> batch)
{
    using (var transaction = await _db.BeginTransactionAsync())
    {
        foreach (var (aggregateId, events) in batch)
        {
            await SaveEventsAsync(aggregateId, events);
        }
        
        await transaction.CommitAsync();
    }
}
```

## Migration Strategy

### Adding Fields to Aggregates

1. Add field to aggregate class
2. Update event handlers to populate field
3. Add migration to replay historical events
4. Mark old handling code with deprecation notice

### Event Versioning

```csharp
public abstract class DomainEvent
{
    public string EventType { get; set; }
    public int EventVersion { get; set; } = 1; // Add versioning
    public string AggregateId { get; set; }
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## Summary

The architecture provides:
- ✅ Event sourcing for complete history
- ✅ CQRS for independent scaling
- ✅ Snapshots for performance
- ✅ Projections for optimized reads
- ✅ Type-safe, async-first design
- ✅ Extensible via decorators
- ✅ Testable in isolation
- ✅ Production-ready error handling
