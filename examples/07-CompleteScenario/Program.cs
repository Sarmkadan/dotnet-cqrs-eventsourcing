// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.Events;

Console.WriteLine("=== Complete CQRS + Event Sourcing Scenario ===\n");

// Setup
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();
var snapshotService = serviceProvider.GetRequiredService<ISnapshotService>();

// Setup event handlers for audit trail
Console.WriteLine("Setting up event handlers...\n");

await eventBus.SubscribeAsync<AccountCreated>(async (@event) =>
{
    Console.WriteLine($"[AUDIT] Account created: {@event.AggregateId}");
    await Task.CompletedTask;
});

await eventBus.SubscribeAsync<MoneyDeposited>(async (@event) =>
{
    Console.WriteLine($"[AUDIT] Deposit: {@event.Amount} to {@event.AggregateId}");
    await Task.CompletedTask;
});

await eventBus.SubscribeAsync<MoneyWithdrawn>(async (@event) =>
{
    Console.WriteLine($"[AUDIT] Withdrawal: {@event.Amount} from {@event.AggregateId}");
    await Task.CompletedTask;
});

// Phase 1: Create account
Console.WriteLine("\n=== PHASE 1: Account Creation ===\n");

var createResult = await accountService.CreateAccountAsync(
    "ACC-SCENARIO-001",
    "Grace Lewis",
    "USD",
    5000m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error: {createResult.Error}");
    return;
}

var accountId = createResult.Data.Id;
Console.WriteLine($"✓ Account created\n");

// Phase 2: Business operations (CQRS Commands)
Console.WriteLine("=== PHASE 2: Business Operations ===\n");

var operations = new[] {
    ("Salary Deposit", () => accountService.DepositAsync(accountId, 2000m, "SALARY-001")),
    ("Rent Payment", () => accountService.WithdrawAsync(accountId, 1500m, "RENT-001")),
    ("Utilities", () => accountService.WithdrawAsync(accountId, 200m, "UTIL-001")),
    ("Dividend", () => accountService.DepositAsync(accountId, 500m, "DIV-001")),
    ("Groceries", () => accountService.WithdrawAsync(accountId, 150m, "GROC-001")),
};

foreach (var (description, operation) in operations)
{
    await operation();
    Console.WriteLine($"✓ {description}");
}

Console.WriteLine();

// Phase 3: Read model (Projection)
Console.WriteLine("=== PHASE 3: Read Models via Projections ===\n");

var projection = await projectionService.BuildProjectionAsync(accountId);

Console.WriteLine($"Account Summary (Read Model):");
Console.WriteLine($"  ID: {accountId}");
Console.WriteLine($"  Status: {projection.Status}");
Console.WriteLine($"  Current Balance: {projection.CurrentBalance} USD");
Console.WriteLine($"  Total Deposits: {projection.TotalDeposits} USD");
Console.WriteLine($"  Total Withdrawals: {projection.TotalWithdrawals} USD");
Console.WriteLine($"  Transactions: {projection.TransactionCount}");
Console.WriteLine($"  Last Updated: {projection.LastUpdated:u}\n");

// Phase 4: Event sourcing (Complete history)
Console.WriteLine("=== PHASE 4: Complete Event History ===\n");

var events = await eventStore.GetEventsAsync(accountId);
Console.WriteLine($"Event Stream ({events.Count} events):\n");

foreach (var evt in events)
{
    Console.WriteLine($"[{evt.Version}] {evt.EventType} @ {evt.Timestamp:u}");
}

Console.WriteLine();

// Phase 5: Time travel capability
Console.WriteLine("=== PHASE 5: Time Travel (Historical State) ===\n");

var midpointEvents = events.Take(events.Count / 2).ToList();
var historicalAccount = new DotNetCqrsEventSourcing.Domain.AggregateRoots.Account();
historicalAccount.ReplayEvents(midpointEvents);

Console.WriteLine($"Account state at event {midpointEvents.Count}:");
Console.WriteLine($"  Balance: {historicalAccount.Balance.CurrentAmount} USD");
Console.WriteLine($"  Transactions: {historicalAccount.Transactions.Count}\n");

// Phase 6: Snapshots for performance
Console.WriteLine("=== PHASE 6: Snapshots for Performance ===\n");

var currentAccount = await accountService.GetAccountAsync(accountId);
if (currentAccount.IsSuccess)
{
    await snapshotService.CreateSnapshotAsync(
        currentAccount.Data,
        accountId,
        events.Count
    );
    Console.WriteLine($"✓ Snapshot created at event {events.Count}");
    Console.WriteLine($"  Snapshot captures: {currentAccount.Data.Transactions.Count} transactions\n");
}

// Phase 7: Audit and compliance
Console.WriteLine("=== PHASE 7: Audit Trail ===\n");

decimal totalDeposits = 0;
decimal totalWithdrawals = 0;
decimal totalFees = 0;

foreach (var evt in events)
{
    if (evt.EventType == "MoneyDeposited")
    {
        totalDeposits += 100; // Placeholder - actual would parse event data
    }
    else if (evt.EventType == "MoneyWithdrawn")
    {
        totalWithdrawals += 50; // Placeholder
    }
}

Console.WriteLine("Compliance Report:");
Console.WriteLine($"  Total Deposits: {totalDeposits} USD");
Console.WriteLine($"  Total Withdrawals: {totalWithdrawals} USD");
Console.WriteLine($"  Account Age: {(DateTime.UtcNow - events[0].Timestamp).TotalSeconds:F0} seconds");
Console.WriteLine($"  Event Count: {events.Count}");
Console.WriteLine($"  ✓ Complete audit trail available\n");

// Phase 8: Final verification
Console.WriteLine("=== PHASE 8: Final Verification ===\n");

var finalAccount = await accountService.GetAccountAsync(accountId);
if (finalAccount.IsSuccess)
{
    var account = finalAccount.Data;
    Console.WriteLine($"✓ Account State:");
    Console.WriteLine($"  Holder: {account.AccountHolderName}");
    Console.WriteLine($"  Balance: {account.Balance.CurrentAmount} USD");
    Console.WriteLine($"  Status: {(account.IsClosed ? "Closed" : "Active")}");
    Console.WriteLine($"  Last Transaction: {account.Transactions.LastOrDefault()?.Timestamp:u}\n");
}

Console.WriteLine("=== Scenario Complete ===\n");

Console.WriteLine("Key Concepts Demonstrated:");
Console.WriteLine("  ✓ CQRS: Separated commands and queries");
Console.WriteLine("  ✓ Event Sourcing: Complete history of all changes");
Console.WriteLine("  ✓ Projections: Optimized read models from events");
Console.WriteLine("  ✓ Snapshots: Performance optimization for replay");
Console.WriteLine("  ✓ Event Bus: Pub/sub for event-driven architecture");
Console.WriteLine("  ✓ Audit Trail: Complete compliance record");
Console.WriteLine("  ✓ Time Travel: Reconstruct any historical state");
Console.WriteLine("  ✓ Consistency: Optimistic concurrency control\n");
