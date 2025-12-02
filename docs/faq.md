# Frequently Asked Questions

## General

### Q: Is Event Sourcing right for my application?

**A:** Event Sourcing is ideal for:
- Applications requiring complete audit trails
- Systems with complex domain logic
- Applications needing time-travel debugging
- Compliance-heavy systems (finance, healthcare)

Event Sourcing may be overkill for:
- Simple CRUD applications
- Read-heavy systems without complex writes
- Applications with no audit requirements

### Q: What's the difference between CQRS and Event Sourcing?

**A:** They solve different problems:

- **CQRS**: Separates read and write models for independent scaling and optimization
- **Event Sourcing**: Stores state as event stream instead of current state snapshot

You can use CQRS without Event Sourcing, and vice versa. This framework combines both for maximum benefit.

### Q: Is this production-ready?

**A:** Yes! The framework includes:
- Comprehensive error handling
- Optimistic concurrency control
- Logging and observability
- Snapshot optimization
- Multiple deployment strategies
- Production configuration examples

### Q: Does the framework support distributed systems?

**A:** Yes. Replace in-process components:
- Event Bus: RabbitMQ, Azure Service Bus, Kafka
- Event Store: SQL Server, PostgreSQL, MongoDB
- Cache: Redis
- Multiple application instances with load balancer

## Architecture

### Q: What's an Aggregate Root?

**A:** An aggregate root is a domain entity that:
- Contains related domain objects
- Acts as consistency boundary
- Raises domain events
- Enforces domain rules

Example: `Account` is an aggregate root containing `Balance` and `Transaction` value objects.

### Q: Why use Value Objects?

**A:** Value Objects:
- Are immutable (prevent accidental changes)
- Have value-based equality (two Money(100, "USD") are equal)
- Contain domain validation (Money rejects negative amounts)
- Make code intent clear (Balance is meaningful, not just decimal)

### Q: What's the difference between commands and events?

**A:**
- **Commands**: Requests to change state ("Deposit $100")
  - May be rejected
  - Only one handler
  - Can fail synchronously

- **Events**: Facts about state changes ("$100 Deposited")
  - Already happened
  - Multiple handlers possible
  - Asynchronously published

### Q: How does optimistic concurrency work?

**A:** Each aggregate has a version number:
```
Load Account: Version 3
Make changes
Save: Check current version = 3? Yes -> Save with Version 4
      Check current version = 3? No -> Reject with ConcurrencyException
```

Thread-safe without locks. Implement retry logic for conflicts.

### Q: What's a projection?

**A:** Projection = Read model built from events.

Example:
```
Events: AccountCreated, Deposited(500), Withdrawn(200)
                        ↓
Projection: { Balance: 1300, TotalDeposits: 500, ... }
                        ↓
Query: "What's the account balance?" -> Fast read from projection
```

## Implementation

### Q: How do I create custom aggregates?

**A:** Inherit from `AggregateRoot`:

```csharp
public class Order : AggregateRoot
{
    public string OrderNumber { get; private set; }
    public List<LineItem> Items { get; private set; }
    
    public void AddItem(string productId, int quantity)
    {
        RaiseEvent(new LineItemAdded(Id, Version + 1, productId, quantity, DateTime.UtcNow));
    }
    
    protected override void Apply(DomainEvent @event)
    {
        switch (@event)
        {
            case LineItemAdded added:
                Items.Add(new LineItem(added.ProductId, added.Quantity));
                break;
        }
    }
}
```

### Q: How do I handle business rule violations?

**A:** Throw `DomainException` in aggregate methods:

```csharp
public void Withdraw(decimal amount)
{
    if (amount <= 0)
        throw new DomainException("Amount must be positive");
    
    if (Balance.CurrentAmount < amount)
        throw new DomainException("Insufficient funds");
    
    RaiseEvent(new MoneyWithdrawn(...));
}
```

Client receives `Result<T>` with error message.

### Q: How do I subscribe to events?

**A:** Use `IEventBus`:

```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Subscribe before publishing
await eventBus.SubscribeAsync<MoneyDeposited>(async (@event) =>
{
    await _emailService.SendDepositNotificationAsync(@event);
});

// Publish later
await eventBus.PublishAsync(depositEvent);
```

### Q: How do I handle sagas (long-running transactions)?

**A:** Implement as event-driven workflow:

```csharp
public class OrderFulfillmentSaga
{
    public async Task HandleOrderPlaced(OrderPlaced @event)
    {
        // Step 1: Reserve inventory
        var reserveResult = await _inventoryService.ReserveAsync(@event.Items);
        
        if (!reserveResult.IsSuccess)
        {
            // Compensating transaction: cancel order
            await _orderService.CancelOrderAsync(@event.OrderId);
            return;
        }
        
        // Step 2: Process payment
        var paymentResult = await _paymentService.ChargeAsync(@event.Amount);
        
        if (!paymentResult.IsSuccess)
        {
            // Compensating transaction: release inventory
            await _inventoryService.ReleaseAsync(@event.Items);
            await _orderService.CancelOrderAsync(@event.OrderId);
            return;
        }
        
        // Step 3: Ship order
        await _shippingService.CreateShipmentAsync(@event.OrderId);
    }
}
```

### Q: How do I rebuild projections?

**A:**
```csharp
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();

// Clear and rebuild all projections
await projectionService.RebuildProjectionsAsync();
```

Use when:
- Projection logic changes
- Read models become corrupted
- Need to add new projection type

## Performance

### Q: Event stream grows unbounded. How to manage size?

**A:** Use snapshots:

```csharp
// Create snapshot every 100 events
const int snapshotInterval = 100;

if (account.Version % snapshotInterval == 0)
{
    await snapshotService.CreateSnapshotAsync(account, account.Id, account.Version);
}
```

On load:
```
Load snapshot (state at event 100)
Replay only events 101-current (instead of 1-current)
```

### Q: Event store queries are slow. Why?

**A:** Missing indexes. For SQL:

```sql
CREATE INDEX IX_Events_AggregateId_Version 
ON Events(AggregateId, Version);

CREATE INDEX IX_Events_EventType 
ON Events(EventType);

CREATE INDEX IX_Events_Timestamp 
ON Events(Timestamp);
```

### Q: How do I improve read performance?

**A:**
1. Use projections (not raw event store queries)
2. Cache projections in Redis
3. Add database read replicas
4. Implement pagination for large result sets
5. Use batch reads

### Q: Memory usage is high. How to optimize?

**A:**
1. Enable snapshots (reduces event replay)
2. Clear projection caches periodically
3. Implement event stream pruning for archived aggregates
4. Use streaming for large event lists
5. Add `using` statements for proper resource cleanup

## Testing

### Q: How do I unit test aggregates?

**A:**
```csharp
[TestMethod]
public void Withdraw_WithSufficientFunds_ReducesBalance()
{
    // Arrange
    var account = new Account();
    account.Apply(new AccountCreated("ACC-001", 1, "John", "USD", 1000m, DateTime.UtcNow));
    
    // Act
    account.Withdraw(200m, "WTH-001");
    
    // Assert
    Assert.AreEqual(800m, account.Balance.CurrentAmount);
}

[TestMethod]
[ExpectedException(typeof(DomainException))]
public void Withdraw_WithInsufficientFunds_ThrowsException()
{
    var account = new Account();
    account.Apply(new AccountCreated("ACC-001", 1, "John", "USD", 100m, DateTime.UtcNow));
    
    // This should throw
    account.Withdraw(200m, "WTH-001");
}
```

### Q: How do I test event sourcing?

**A:**
```csharp
[TestMethod]
public async Task CreateAccount_PersistsEvent()
{
    var eventStore = new InMemoryEventRepository();
    var service = new AccountService(eventStore);
    
    await service.CreateAccountAsync("ACC-001", "John", "USD", 1000m);
    
    var events = await eventStore.GetEventsAsync("ACC-001");
    Assert.IsTrue(events.Any(e => e.EventType == "AccountCreated"));
}
```

### Q: How do I test projections?

**A:**
```csharp
[TestMethod]
public async Task ProjectionBuilds_CorrectTotals()
{
    var events = new List<EventEnvelope>
    {
        CreateEventEnvelope(new MoneyDeposited("ACC-001", 2, 500m, "DEP-001", DateTime.UtcNow)),
        CreateEventEnvelope(new MoneyWithdrawn("ACC-001", 3, 200m, "WTH-001", DateTime.UtcNow))
    };
    
    var projection = new AccountProjection();
    foreach (var @event in events)
    {
        projection.Handle(@event);
    }
    
    Assert.AreEqual(500m, projection.TotalDeposits);
    Assert.AreEqual(200m, projection.TotalWithdrawals);
}
```

## Deployment

### Q: What database should I use?

**A:** Recommendations:

- **SQL Server**: Enterprise, complex queries, strong ACID guarantees
- **PostgreSQL**: Open source, excellent performance, JSON support
- **MongoDB**: Schema-flexible, horizontal scaling, great for event streams

All work well with event sourcing patterns.

### Q: Can I use in-memory store in production?

**A:** No. In-memory store:
- Loses all data on restart
- Doesn't survive server crashes
- Can't scale across instances
- Use only for development/testing

### Q: How do I migrate to Event Sourcing?

**A:** Two strategies:

1. **Lift & Shift** (new system):
   - Start fresh event streams
   - Build projections from events
   - Keep old system running in parallel
   - Migrate after validation

2. **Parallel Running**:
   - Dual writes to old + new system
   - Verify event stream correctness
   - Switch reads after validation complete

### Q: How do I handle event versioning?

**A:** Add `EventVersion` to events:

```csharp
public abstract class DomainEvent
{
    public int EventVersion { get; set; } = 1;
}

public class MoneyDeposited : DomainEvent
{
    // Version 1 events have just Amount
    public decimal Amount { get; set; }
    
    // In application, handle both versions:
    public static MoneyDeposited FromLegacy(Dictionary<string, object> legacyData)
    {
        return new MoneyDeposited
        {
            Amount = (decimal)legacyData["amount"],
            EventVersion = 1
        };
    }
}
```

### Q: How do I backup the event store?

**A:**
```bash
# SQL Server
BACKUP DATABASE [CqrsEventStore] TO DISK = 'C:\backup\cqrs.bak'

# PostgreSQL
pg_dump cqrs_event_store > backup.sql

# MongoDB
mongodump -d cqrs_db -o backup/
```

### Q: Do I need monitoring?

**A:** Absolutely. Monitor:
- Event store latency
- Event publishing lag
- Projection rebuild time
- Snapshot intervals
- Cache hit rates
- Concurrency exception rate

## Troubleshooting

### Q: OptimisticConcurrencyException keeps happening

**A:** Implement retry with exponential backoff:

```csharp
const int maxRetries = 3;
const int baseDelayMs = 100;

for (int attempt = 0; attempt < maxRetries; attempt++)
{
    try
    {
        await _eventStore.SaveEventsAsync(aggregateId, version, events);
        return;
    }
    catch (OptimisticConcurrencyException) when (attempt < maxRetries - 1)
    {
        await Task.Delay(baseDelayMs * (int)Math.Pow(2, attempt));
        // Reload aggregate and retry
    }
}
```

### Q: Events not appearing in projections

**A:** Check:
1. Handlers subscribed? `await eventBus.SubscribeAsync<T>(...)`
2. Events published? `await eventBus.PublishAsync(...)`
3. Handler async method completing? `await Task.CompletedTask`
4. Projection service called? `BuildProjectionAsync()`

### Q: Event store queries timeout

**A:** Add indexes:
```sql
CREATE INDEX idx_aggregate ON events(aggregate_id);
CREATE INDEX idx_version ON events(aggregate_id, version);
```

### Q: Memory growing unbounded

**A:** 
1. Enable snapshots
2. Clear caches: `await _cache.FlushAsync()`
3. Archive old events (move to cold storage)
4. Rebuild projections to remove stale data

## Getting Help

- **GitHub Issues**: Report bugs and request features
- **Documentation**: Check `docs/` directory
- **Examples**: Run example programs in `examples/`
- **Architecture Guide**: See `docs/architecture.md`
- **API Reference**: See `docs/api-reference.md`

---

Still have questions? Open an issue on GitHub!
