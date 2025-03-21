// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.Events;

Console.WriteLine("=== Event Handling Example ===\n");

// Setup dependency injection
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Subscribe to events before they are published
Console.WriteLine("1. Subscribing to events...\n");

await eventBus.SubscribeAsync<AccountCreated>(async (@event) =>
{
    Console.WriteLine($"[EVENT HANDLER] Account Created");
    Console.WriteLine($"  Account ID: {@event.AggregateId}");
    Console.WriteLine($"  Holder: {@event.AccountHolderName}");
    Console.WriteLine($"  Initial Balance: {@event.InitialBalance} {@event.Currency}");
    Console.WriteLine($"  Timestamp: {@event.Timestamp:u}\n");
    await Task.CompletedTask;
});

await eventBus.SubscribeAsync<MoneyDeposited>(async (@event) =>
{
    Console.WriteLine($"[EVENT HANDLER] Money Deposited");
    Console.WriteLine($"  Account ID: {@event.AggregateId}");
    Console.WriteLine($"  Amount: {@event.Amount}");
    Console.WriteLine($"  Reference: {@event.Reference}\n");
    await Task.CompletedTask;
});

await eventBus.SubscribeAsync<MoneyWithdrawn>(async (@event) =>
{
    Console.WriteLine($"[EVENT HANDLER] Money Withdrawn");
    Console.WriteLine($"  Account ID: {@event.AggregateId}");
    Console.WriteLine($"  Amount: {@event.Amount}");
    Console.WriteLine($"  Reference: {@event.Reference}\n");
    await Task.CompletedTask;
});

// Now trigger events
Console.WriteLine("2. Creating account (triggers AccountCreated event)...\n");
var createResult = await accountService.CreateAccountAsync(
    "ACC-EVENTS-001",
    "Bob Smith",
    "USD",
    1000m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error: {createResult.Error}");
    return;
}

// Simulate small delay for event processing
await Task.Delay(100);

Console.WriteLine("3. Depositing funds (triggers MoneyDeposited event)...\n");
await accountService.DepositAsync("ACC-EVENTS-001", 500m, "DEP-001");

await Task.Delay(100);

Console.WriteLine("4. Withdrawing funds (triggers MoneyWithdrawn event)...\n");
await accountService.WithdrawAsync("ACC-EVENTS-001", 200m, "WTH-001");

await Task.Delay(100);

// Verify final state
Console.WriteLine("5. Final account state...");
var finalResult = await accountService.GetAccountAsync("ACC-EVENTS-001");
if (finalResult.IsSuccess)
{
    var account = finalResult.Data;
    Console.WriteLine($"  Balance: {account.Balance.CurrentAmount} USD");
    Console.WriteLine($"  Total Transactions: {account.Transactions.Count}");
}

Console.WriteLine("\n=== Example Complete ===");
