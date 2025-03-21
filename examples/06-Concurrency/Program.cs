// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;

Console.WriteLine("=== Concurrency Example ===\n");

// Setup dependency injection
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();

// Create account
Console.WriteLine("1. Creating account...\n");
var createResult = await accountService.CreateAccountAsync(
    "ACC-CONCURRENT-001",
    "Frank Davis",
    "USD",
    1000m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error: {createResult.Error}");
    return;
}

Console.WriteLine($"✓ Account created with balance: 1000 USD\n");

// Test case 1: Sequential operations (no conflicts)
Console.WriteLine("2. Sequential operations (no conflicts)...\n");

var deposit1 = await accountService.DepositAsync("ACC-CONCURRENT-001", 100m, "DEP-001");
Console.WriteLine($"  Deposit 1: {(deposit1.IsSuccess ? "✓ Success" : "✗ Failed")}");

var deposit2 = await accountService.DepositAsync("ACC-CONCURRENT-001", 100m, "DEP-002");
Console.WriteLine($"  Deposit 2: {(deposit2.IsSuccess ? "✓ Success" : "✗ Failed")}");

var withdraw1 = await accountService.WithdrawAsync("ACC-CONCURRENT-001", 50m, "WTH-001");
Console.WriteLine($"  Withdraw 1: {(withdraw1.IsSuccess ? "✓ Success" : "✗ Failed")}\n");

// Test case 2: Simulate concurrent operations
Console.WriteLine("3. Simulating concurrent operations...\n");

var tasks = new List<Task<dynamic>>();

// Create 5 concurrent deposit operations
for (int i = 0; i < 5; i++)
{
    var index = i;
    var task = Task.Run(async () =>
    {
        var amount = 100m;
        var reference = $"DEP-CONCURRENT-{index:000}";
        var result = await accountService.DepositAsync("ACC-CONCURRENT-001", amount, reference);
        return new { Index = index, Result = result };
    });
    tasks.Add(task);
}

// Wait for all concurrent operations
var results = await Task.WhenAll(tasks);

int successful = 0;
int failed = 0;

foreach (var result in results)
{
    if (result.Result.IsSuccess)
    {
        successful++;
        Console.WriteLine($"  Deposit {result.Index}: ✓ Success (new balance: {result.Result.Data})");
    }
    else
    {
        failed++;
        Console.WriteLine($"  Deposit {result.Index}: ✗ Failed ({result.Result.Error})");
    }
}

Console.WriteLine($"\n  Results: {successful} succeeded, {failed} failed\n");

// Test case 3: Version tracking
Console.WriteLine("4. Checking version tracking...\n");
var allEvents = await eventStore.GetEventsAsync("ACC-CONCURRENT-001");
Console.WriteLine($"  Total events in stream: {allEvents.Count}");
Console.WriteLine($"  Event versions are sequential:");

foreach (var evt in allEvents.TakeLast(3))
{
    Console.WriteLine($"    Version {evt.Version}: {evt.EventType}");
}

Console.WriteLine();

// Test case 4: Final state verification
Console.WriteLine("5. Verifying final state consistency...\n");
var finalResult = await accountService.GetAccountAsync("ACC-CONCURRENT-001");
if (finalResult.IsSuccess)
{
    var finalAccount = finalResult.Data;
    Console.WriteLine($"  Final balance: {finalAccount.Balance.CurrentAmount} USD");
    Console.WriteLine($"  Total transactions: {finalAccount.Transactions.Count}");

    // Calculate expected balance
    decimal expectedBalance = 1000m; // Initial
    expectedBalance += 100m; // DEP-001
    expectedBalance += 100m; // DEP-002
    expectedBalance -= 50m;  // WTH-001
    expectedBalance += 500m; // 5 concurrent deposits

    Console.WriteLine($"  Expected balance: {expectedBalance} USD");
    Console.WriteLine($"  ✓ Consistency verified: {Math.Abs(finalAccount.Balance.CurrentAmount - expectedBalance) < 0.01m}\n");
}

// Test case 5: Optimistic concurrency explanation
Console.WriteLine("6. Understanding optimistic concurrency...\n");
Console.WriteLine("  How it works:");
Console.WriteLine("  1. Each aggregate has a version number");
Console.WriteLine("  2. When saving events, we check if version hasn't changed");
Console.WriteLine("  3. If another process updated it, save fails with ConcurrencyException");
Console.WriteLine("  4. Implement retry logic to handle conflicts gracefully");
Console.WriteLine();
Console.WriteLine("  Benefits:");
Console.WriteLine("  ✓ No database locks needed");
Console.WriteLine("  ✓ Better scalability and throughput");
Console.WriteLine("  ✓ Conflicts are rare in most business domains");
Console.WriteLine("  ✓ Simple to implement and understand\n");

Console.WriteLine("=== Example Complete ===");
