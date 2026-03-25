#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.ReadModels;

Console.WriteLine("=== v2 Basic Usage Example ===\n");

// Setup dependency injection with v2 features
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();

// Example 1: Create an account with v2 features
Console.WriteLine("1. Creating account with v2 features...");
var createResult = await accountService.CreateAccountAsync(
    accountId: "ACC-V2-001",
    accountHolderName: "Bob Smith",
    currency: "EUR",
    initialBalance: 10000m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error: {createResult.Error}");
    return;
}

var account = createResult.Data;
Console.WriteLine($"✓ Account created: {account.Id}");
Console.WriteLine($" Balance: {account.Balance.CurrentAmount} {account.Balance.Currency}\n");

// Example 2: Deposit funds
Console.WriteLine("2. Depositing funds...");
var depositResult = await accountService.DepositAsync(
    accountId: account.Id,
    amount: 5000m,
    referenceNumber: "DEP-V2-001"
);

if (depositResult.IsSuccess)
{
    Console.WriteLine($"✓ Deposit successful");
    Console.WriteLine($" New balance: {depositResult.Data} EUR\n");
}

// Example 3: Use projections (v2 feature - eventually consistent materialized views)
Console.WriteLine("3. Using projections for optimized reads...");
var readModelQueryService = serviceProvider.GetRequiredService<AccountReadModelQueryService>();

// Wait for projections to catch up (eventual consistency)
Console.WriteLine("Waiting for projections to update...");
await Task.Delay(500); // Small delay for projection updates

// Query using projection (much faster than reading event stream)
var accountProjection = await readModelQueryService.GetAccountByIdAsync(account.Id);
if (accountProjection is not null)
{
    Console.WriteLine($"✓ Projection retrieved");
    Console.WriteLine($" ID: {accountProjection.Id}");
    Console.WriteLine($" Holder: {accountProjection.AccountHolderName}");
    Console.WriteLine($" Balance: {accountProjection.Balance} {accountProjection.Currency}");
    Console.WriteLine($" Transaction Count: {accountProjection.TransactionCount}");
    Console.WriteLine();
}

// Example 4: Get current account state
Console.WriteLine("4. Retrieving current account state...");
var getResult = await accountService.GetAccountAsync(account.Id);
if (getResult.IsSuccess)
{
    var updated = getResult.Data;
    Console.WriteLine($"✓ Account state retrieved");
    Console.WriteLine($" ID: {updated.Id}");
    Console.WriteLine($" Holder: {updated.AccountHolderName}");
    Console.WriteLine($" Balance: {updated.Balance.CurrentAmount} {updated.Balance.Currency}");
    Console.WriteLine();
}

// Example 5: Query all accounts using projection (v2 feature)
Console.WriteLine("5. Querying all accounts using projection...");
var allAccounts = await readModelQueryService.GetAllAccountsAsync();
Console.WriteLine($"✓ Found {allAccounts.Count} accounts via projection");
foreach (var acc in allAccounts)
{
    Console.WriteLine($" - {acc.Id}: {acc.AccountHolderName} - {acc.Balance} {acc.Currency}");
}
Console.WriteLine();

// Example 6: View event stream (traditional approach)
Console.WriteLine("6. Viewing event stream (traditional)...");
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var events = await eventStore.GetEventsAsync(account.Id);
Console.WriteLine($"✓ Event stream retrieved ({events.Count} events)");
foreach (var envelope in events)
{
    Console.WriteLine($" [{envelope.Version}] {envelope.EventType} @ {envelope.Timestamp:u}");
}

Console.WriteLine("\n=== v2 Example Complete ===");
Console.WriteLine("\nKey v2 features demonstrated:");
Console.WriteLine("- Projection-based read models (eventually consistent materialized views)");
Console.WriteLine("- Optimized queries using projections");
Console.WriteLine("- Read model query service integration");