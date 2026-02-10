# Examples - CQRS + Event Sourcing Framework

Complete, runnable examples demonstrating all aspects of the CQRS + Event Sourcing framework.

## Quick Start

Run any example:

```bash
cd 01-BasicAccount
dotnet run
```

## Examples

### 01-BasicAccount
**What it teaches:** Basic account operations and state management

- Create an account with initial balance
- Deposit and withdraw funds
- Retrieve current account state
- View the event stream

**Key concepts:**
- Aggregates and commands
- Domain state transitions
- Event persistence

**Run:** `dotnet run`

---

### 02-EventHandling
**What it teaches:** Event-driven architecture and pub/sub messaging

- Subscribe to domain events
- Publish events from operations
- Multiple handlers per event type
- Async event processing

**Key concepts:**
- Event bus and pub/sub
- Event handlers
- Event-driven workflows
- Asynchronous processing

**Run:** `dotnet run`

---

### 03-Projections
**What it teaches:** Read models and optimized queries

- Build projections (read models) from events
- Query optimized read models instead of event streams
- Calculate statistics from projections
- Performance benefits of projections

**Key concepts:**
- Read model separation (CQRS)
- Projection building
- Query optimization
- Denormalization for reads

**Run:** `dotnet run`

---

### 04-EventReplay
**What it teaches:** Event sourcing and reconstructing history

- Replay events to reconstruct state
- Time-travel to historical states
- Create snapshots for performance
- Load from snapshots efficiently

**Key concepts:**
- Event sourcing
- Event replay
- Snapshots
- Efficient aggregate loading
- Historical state reconstruction

**Run:** `dotnet run`

---

### 05-ErrorHandling
**What it teaches:** Error handling and the Result pattern

- Prevent invalid operations via domain validation
- Handle domain exceptions gracefully
- Use Result<T> for explicit error handling
- Distinguish between errors and exceptions

**Key concepts:**
- Domain-driven error handling
- Result pattern
- Validation decorators
- Error propagation
- No exception abuse

**Run:** `dotnet run`

---

### 06-Concurrency
**What it teaches:** Handling concurrent operations safely

- Optimistic concurrency control
- Version-based conflict detection
- Sequential vs. concurrent operations
- Consistency verification

**Key concepts:**
- Optimistic locking
- Version numbers
- Conflict detection
- Retry strategies
- Concurrent command handling

**Run:** `dotnet run`

---

### 07-CompleteScenario
**What it teaches:** End-to-end application using all features

- Real-world account lifecycle scenario
- Event handlers and audit trail
- Projections for reporting
- Snapshots for performance
- Time-travel capabilities
- Compliance and auditing

**Key concepts:**
- Complete architecture
- CQRS in practice
- Event sourcing benefits
- Audit trails
- Historical queries
- All features working together

**Run:** `dotnet run`

---

## Running All Examples

```bash
make examples
```

Or run individually:

```bash
cd 01-BasicAccount && dotnet run && cd ..
cd 02-EventHandling && dotnet run && cd ..
cd 03-Projections && dotnet run && cd ..
cd 04-EventReplay && dotnet run && cd ..
cd 05-ErrorHandling && dotnet run && cd ..
cd 06-Concurrency && dotnet run && cd ..
cd 07-CompleteScenario && dotnet run && cd ..
```

## Learning Path

Recommended order for learning:

1. **01-BasicAccount** - Understand basic account operations
2. **02-EventHandling** - Learn about event-driven architecture
3. **03-Projections** - See how to optimize reads
4. **04-EventReplay** - Understand event sourcing benefits
5. **05-ErrorHandling** - Learn proper error handling
6. **06-Concurrency** - Handle concurrent operations
7. **07-CompleteScenario** - See everything working together

## Creating Your Own Examples

To create a new example:

1. Create a new directory: `mkdir 08-MyExample`
2. Create Program.cs with your example code
3. Create 08-MyExample.csproj with project reference
4. Run with `dotnet run`

Example project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../../dotnet-cqrs-eventsourcing.csproj" />
  </ItemGroup>
</Project>
```

## Common Patterns

### Setup DI

```csharp
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();
```

### Get Services

```csharp
var accountService = serviceProvider.GetRequiredService<IAccountService>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
```

### Subscribe to Events

```csharp
await eventBus.SubscribeAsync<MoneyDeposited>(async (@event) =>
{
    Console.WriteLine($"Deposit: {@event.Amount}");
    await Task.CompletedTask;
});
```

### Handle Results

```csharp
var result = await accountService.DepositAsync(accountId, amount, reference);
if (result.IsSuccess)
{
    Console.WriteLine($"New balance: {result.Data}");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}
```

## Troubleshooting

**Q: NullReferenceException when running example**
A: Make sure you're in the example directory and running `dotnet run`

**Q: Project reference error**
A: Verify the relative path in .csproj is correct: `../../dotnet-cqrs-eventsourcing.csproj`

**Q: No events appearing**
A: Ensure you subscribe to events before publishing them

**Q: Build fails with compilation errors**
A: Ensure you're using .NET 10 SDK: `dotnet --version`

## Further Learning

After running the examples:

1. **Read the documentation:**
   - `docs/getting-started.md` - Guided tutorial
   - `docs/architecture.md` - Deep architectural guide
   - `docs/api-reference.md` - Complete API docs
   - `docs/deployment.md` - Production deployment
   - `docs/faq.md` - Common questions

2. **Study the framework code:**
   - `Domain/AggregateRoots/Account.cs` - Aggregate implementation
   - `Application/Services/AccountService.cs` - Service layer
   - `Application/Services/EventStore.cs` - Event persistence
   - `Infrastructure/` - Cross-cutting concerns

3. **Explore advanced topics:**
   - Custom aggregates
   - Event versioning
   - Saga patterns
   - Multi-tenant support
   - Distributed deployments

## Contributing Examples

Have a great example? Contribute it!

1. Create a new numbered directory
2. Implement the example with clear comments
3. Update this README
4. Submit a pull request

---

**Author:** Vladyslav Zaiets - CTO & Software Architect  
**Website:** https://sarmkadan.com  
**GitHub:** https://github.com/Sarmkadan
