// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;

Console.WriteLine("=== Event Replay Example ===\n");

// Setup dependency injection
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var snapshotService = serviceProvider.GetRequiredService<ISnapshotService>();

// Create account with multiple transactions
Console.WriteLine("1. Creating account with multiple transactions...\n");

var createResult = await accountService.CreateAccountAsync(
    "ACC-REPLAY-001",
    "David Brown",
    "USD",
    1000m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error: {createResult.Error}");
    return;
}

var accountId = createResult.Data.Id;

// Perform transactions
for (int i = 0; i < 5; i++)
{
    await accountService.DepositAsync(accountId, 100m, $"DEP-{i:000}");
    await accountService.WithdrawAsync(accountId, 50m, $"WTH-{i:000}");
}

Console.WriteLine($"✓ Created account with 11 events (1 create + 5 deposits + 5 withdrawals)\n");

// Retrieve all events
Console.WriteLine("2. Retrieving complete event stream...\n");
var allEvents = await eventStore.GetEventsAsync(accountId);
Console.WriteLine($"✓ Retrieved {allEvents.Count} events\n");

// Replay to reconstruct state at each point in time
Console.WriteLine("3. Replaying events to show account state at each step...\n");

var replayAccount = new Account();

for (int i = 0; i < allEvents.Count; i++)
{
    var currentEvent = allEvents[i];

    // Replay this event
    var eventsUntilNow = allEvents.Take(i + 1).ToList();
    var reconstructed = new Account();
    reconstructed.ReplayEvents(eventsUntilNow);

    Console.WriteLine($"After event {i + 1} ({currentEvent.EventType}):");
    Console.WriteLine($"  Balance: {reconstructed.Balance.CurrentAmount} USD");

    // Show state transitions
    if (i > 0)
    {
        var previousBalance = new Account().ReplayEvents(allEvents.Take(i).ToList()).Balance.CurrentAmount;
        var change = reconstructed.Balance.CurrentAmount - previousBalance;
        Console.WriteLine($"  Change: {(change > 0 ? "+" : "")}{change} USD");
    }
    Console.WriteLine();
}

// Create a snapshot
Console.WriteLine("4. Creating snapshot at midpoint...\n");
int midpoint = allEvents.Count / 2;
var snapshotAccount = new Account();
snapshotAccount.ReplayEvents(allEvents.Take(midpoint).ToList());

await snapshotService.CreateSnapshotAsync(snapshotAccount, accountId, midpoint);
Console.WriteLine($"✓ Snapshot created at event {midpoint}");
Console.WriteLine($"  Snapshot balance: {snapshotAccount.Balance.CurrentAmount} USD\n");

// Demonstrate efficient replay from snapshot
Console.WriteLine("5. Loading account efficiently using snapshot...\n");

var snapshot = await snapshotService.GetSnapshotAsync(accountId);
if (snapshot != null)
{
    var accountFromSnapshot = snapshot.RestoreAggregate();
    var eventsAfterSnapshot = allEvents.Skip(midpoint).ToList();
    accountFromSnapshot.ReplayEvents(eventsAfterSnapshot);

    Console.WriteLine($"✓ Loaded from snapshot at event {midpoint}");
    Console.WriteLine($"  Only replayed {eventsAfterSnapshot.Count} events instead of {allEvents.Count}");
    Console.WriteLine($"  Final balance: {accountFromSnapshot.Balance.CurrentAmount} USD\n");
}

// Show alternative history (replay subset of events)
Console.WriteLine("6. Time-travel: Balance after 3 events...\n");
var firstThreeEvents = allEvents.Take(3).ToList();
var accountAtThree = new Account();
accountAtThree.ReplayEvents(firstThreeEvents);

Console.WriteLine($"✓ Account state after first 3 events:");
Console.WriteLine($"  Balance: {accountAtThree.Balance.CurrentAmount} USD");
Console.WriteLine($"  Transactions: {accountAtThree.Transactions.Count}\n");

Console.WriteLine("=== Example Complete ===");
