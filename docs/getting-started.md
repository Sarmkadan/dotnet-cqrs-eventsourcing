# Getting Started with CQRS + Event Sourcing

A step-by-step guide to get your first CQRS + Event Sourcing application running.

## Prerequisites

- .NET 10 SDK ([Download](https://dotnet.microsoft.com/download))
- A code editor (VS Code, Visual Studio, or Rider)
- Basic C# knowledge

## Step 1: Create a New Project

```bash
dotnet new console -n MyFirstCqrsApp
cd MyFirstCqrsApp
```

## Step 2: Add Project Reference

Clone the framework:
```bash
cd ..
git clone https://github.com/sarmkadan/dotnet-cqrs-eventsourcing.git
cd MyFirstCqrsApp
dotnet add reference ../dotnet-cqrs-eventsourcing/dotnet-cqrs-eventsourcing.csproj
```

## Step 3: Update Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;

var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();

// Create account
var createResult = await accountService.CreateAccountAsync(
    "ACC-001",
    "John Developer",
    "USD",
    5000m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error: {createResult.Error}");
    return;
}

var account = createResult.Data;
Console.WriteLine($"✓ Account created: {account.Id}");
Console.WriteLine($"  Initial balance: {account.Balance.CurrentAmount} {account.Balance.Currency}");

// Perform a deposit
var depositResult = await accountService.DepositAsync(
    account.Id,
    1000m,
    "DEP-001"
);

if (depositResult.IsSuccess)
{
    Console.WriteLine($"✓ Deposit successful: {depositResult.Data}");
}

// Perform a withdrawal
var withdrawResult = await accountService.WithdrawAsync(
    account.Id,
    500m,
    "WTH-001"
);

if (withdrawResult.IsSuccess)
{
    Console.WriteLine($"✓ Withdrawal successful: {withdrawResult.Data}");
}

// Get current state
var getResult = await accountService.GetAccountAsync(account.Id);
if (getResult.IsSuccess)
{
    var updated = getResult.Data;
    Console.WriteLine($"\nFinal account state:");
    Console.WriteLine($"  Balance: {updated.Balance.CurrentAmount} {updated.Balance.Currency}");
    Console.WriteLine($"  Transactions: {updated.Transactions.Count}");
}

// View events
var events = await eventStore.GetEventsAsync(account.Id);
Console.WriteLine($"\nEvent stream ({events.Count} events):");
foreach (var envelope in events)
{
    Console.WriteLine($"  [{envelope.Version}] {envelope.EventType} @ {envelope.Timestamp:u}");
}
```

## Step 4: Run the Application

```bash
dotnet run
```

Expected output:
```
✓ Account created: ACC-001
  Initial balance: 5000 USD
✓ Deposit successful: 6000
✓ Withdrawal successful: 5500

Final account state:
  Balance: 5500 USD
  Transactions: 2

Event stream (3 events):
  [1] AccountCreated @ 2024-01-15T10:30:45Z
  [2] MoneyDeposited @ 2024-01-15T10:30:45Z
  [3] MoneyWithdrawn @ 2024-01-15T10:30:45Z
```

## Next Steps

### Learn the Core Concepts

1. **Aggregates**: Read `Domain/AggregateRoots/Account.cs` to understand aggregate roots
2. **Events**: Check `Domain/Events/AccountEvents.cs` for event definitions
3. **Services**: Explore `Application/Services/AccountService.cs` for business logic

### Create Custom Aggregates

```csharp
using DotNetCqrsEventSourcing.Domain.AggregateRoots;
using DotNetCqrsEventSourcing.Domain.Events;

public class Order : AggregateRoot
{
    public string OrderNumber { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();
    public OrderStatus Status { get; private set; }
    
    public void PlaceOrder(string orderNumber)
    {
        ApplyEvent(new OrderPlaced(
            Id, 
            Version + 1,
            orderNumber,
            DateTime.UtcNow
        ));
    }
    
    public void AddItem(string productId, int quantity, decimal price)
    {
        ApplyEvent(new ItemAdded(
            Id,
            Version + 1,
            productId,
            quantity,
            price,
            DateTime.UtcNow
        ));
    }
}

public class OrderPlaced : DomainEvent
{
    public string OrderNumber { get; set; }
    
    public OrderPlaced(string aggregateId, int version, 
        string orderNumber, DateTime timestamp)
        : base(aggregateId, version, timestamp)
    {
        OrderNumber = orderNumber;
    }
}
```

### Subscribe to Events

```csharp
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

await eventBus.SubscribeAsync<MoneyDeposited>(async (@event) =>
{
    Console.WriteLine($"Account {event.AggregateId} received deposit of {event.Amount}");
    // Send notification, update read model, etc.
    await Task.CompletedTask;
});
```

### Build Read Models with Projections

```csharp
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();

// Build projection
var projection = await projectionService.BuildProjectionAsync("ACC-001");

// Use optimized read model
Console.WriteLine($"Account Status: {projection.Status}");
Console.WriteLine($"Total Deposits: {projection.TotalDeposits}");
Console.WriteLine($"Total Withdrawals: {projection.TotalWithdrawals}");
```

### Use Snapshots for Performance

```csharp
var snapshotService = serviceProvider.GetRequiredService<ISnapshotService>();

// After processing events, create a snapshot
await snapshotService.CreateSnapshotAsync(account, "ACC-001", 100);

// Later, load from snapshot and only replay newer events
var snapshot = await snapshotService.GetSnapshotAsync("ACC-001");
if (snapshot != null)
{
    // Only replay events after snapshot version
    var recentEvents = await eventStore.GetEventsAsync("ACC-001", snapshot.Version);
}
```

## Common Patterns

### Error Handling

```csharp
var result = await accountService.WithdrawAsync("ACC-001", 10000m, "ref");

if (!result.IsSuccess)
{
    // Handle error
    Console.WriteLine($"Operation failed: {result.Error}");
}
else
{
    Console.WriteLine($"New balance: {result.Data}");
}
```

### Testing Event Sourcing

```csharp
[TestMethod]
public async Task TestAccountCreation()
{
    var accountService = serviceProvider.GetRequiredService<IAccountService>();
    
    var result = await accountService.CreateAccountAsync(
        "TEST-001", "Test", "USD", 1000m
    );
    
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual(1000m, result.Data.Balance.CurrentAmount);
}

[TestMethod]
public async Task TestEventStoreIntegrity()
{
    var eventStore = serviceProvider.GetRequiredService<IEventStore>();
    
    // Create account
    var result = await accountService.CreateAccountAsync(
        "TEST-002", "Test", "USD", 1000m
    );
    
    // Verify event was stored
    var events = await eventStore.GetEventsAsync("TEST-002");
    Assert.IsTrue(events.Any(e => e.EventType == nameof(AccountCreated)));
}
```

### Replaying Events

```csharp
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var events = await eventStore.GetEventsAsync("ACC-001");

// Reconstruct state at specific point
var account = new Account();
var stateAtEvent5 = account.ReplayEvents(events.Take(5).ToList());

Console.WriteLine($"Balance after 5 events: {stateAtEvent5.Balance.CurrentAmount}");
```

## Troubleshooting

**Q: DependencyInjectionException: Unable to resolve IEventStore**
A: Make sure to call `services.AddCqrsFramework()` before building the service provider.

**Q: OptimisticConcurrencyException when creating accounts**
A: Check that you're using unique account IDs.

**Q: Events not appearing in event store**
A: Verify the account service call returned success and the events were not filtered.

## Resources

- Full API Reference: See `docs/api-reference.md`
- Architecture Deep Dive: See `docs/architecture.md`
- Advanced Examples: See `examples/` directory

## Next: Explore Examples

Run the example programs:

```bash
cd ../examples
dotnet run --project 01-BasicAccount/
dotnet run --project 02-EventHandling/
dotnet run --project 03-Projections/
```

Happy coding! 🎉
