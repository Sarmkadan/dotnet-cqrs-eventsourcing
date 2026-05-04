// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;

Console.WriteLine("=== Basic Account Example ===\n");

// Setup dependency injection
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();

// Example 1: Create an account
Console.WriteLine("1. Creating account...");
var createResult = await accountService.CreateAccountAsync(
    accountId: "ACC-2024-001",
    accountHolderName: "Alice Johnson",
    currency: "USD",
    initialBalance: 5000m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error: {createResult.Error}");
    return;
}

var account = createResult.Data;
Console.WriteLine($"✓ Account created: {account.Id}");
Console.WriteLine($"  Balance: {account.Balance.CurrentAmount} {account.Balance.Currency}\n");

// Example 2: Deposit funds
Console.WriteLine("2. Depositing funds...");
var depositResult = await accountService.DepositAsync(
    accountId: account.Id,
    amount: 2500m,
    referenceNumber: "DEP-2024-001"
);

if (depositResult.IsSuccess)
{
    Console.WriteLine($"✓ Deposit successful");
    Console.WriteLine($"  New balance: {depositResult.Data} USD\n");
}

// Example 3: Withdraw funds
Console.WriteLine("3. Withdrawing funds...");
var withdrawResult = await accountService.WithdrawAsync(
    accountId: account.Id,
    amount: 1000m,
    referenceNumber: "WTH-2024-001"
);

if (withdrawResult.IsSuccess)
{
    Console.WriteLine($"✓ Withdrawal successful");
    Console.WriteLine($"  New balance: {withdrawResult.Data} USD\n");
}

// Example 4: Get current account state
Console.WriteLine("4. Retrieving current account state...");
var getResult = await accountService.GetAccountAsync(account.Id);
if (getResult.IsSuccess)
{
    var updated = getResult.Data;
    Console.WriteLine($"✓ Account state retrieved");
    Console.WriteLine($"  ID: {updated.Id}");
    Console.WriteLine($"  Holder: {updated.AccountHolderName}");
    Console.WriteLine($"  Balance: {updated.Balance.CurrentAmount} {updated.Balance.Currency}");
    Console.WriteLine($"  Transactions: {updated.Transactions.Count}");
    Console.WriteLine();

    Console.WriteLine("  Transaction history:");
    foreach (var txn in updated.Transactions)
    {
        Console.WriteLine($"    - {txn.Type}: {txn.Amount} USD (Ref: {txn.Reference})");
    }
    Console.WriteLine();
}

// Example 5: View event stream
Console.WriteLine("5. Viewing event stream...");
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var events = await eventStore.GetEventsAsync(account.Id);

Console.WriteLine($"✓ Event stream retrieved ({events.Count} events)");
foreach (var envelope in events)
{
    Console.WriteLine($"  [{envelope.Version}] {envelope.EventType} @ {envelope.Timestamp:u}");
}

Console.WriteLine("\n=== Example Complete ===");
