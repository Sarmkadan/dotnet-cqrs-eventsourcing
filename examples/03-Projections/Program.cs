// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;

Console.WriteLine("=== Projections Example ===\n");

// Setup dependency injection
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();

// Create account with transactions
Console.WriteLine("1. Creating account and performing transactions...\n");

var createResult = await accountService.CreateAccountAsync(
    "ACC-PROJ-001",
    "Carol White",
    "USD",
    2000m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error: {createResult.Error}");
    return;
}

var account = createResult.Data;
Console.WriteLine($"✓ Account created with initial balance: {account.Balance.CurrentAmount} USD\n");

// Perform multiple transactions
Console.WriteLine("2. Performing transactions...");
var deposits = new[] { 1000m, 500m, 250m };
var withdrawals = new[] { 300m, 200m };

int depIndex = 0;
foreach (var amount in deposits)
{
    await accountService.DepositAsync(account.Id, amount, $"DEP-{depIndex++:000}");
    Console.WriteLine($"  ✓ Deposited {amount} USD");
}

int wthIndex = 0;
foreach (var amount in withdrawals)
{
    await accountService.WithdrawAsync(account.Id, amount, $"WTH-{wthIndex++:000}");
    Console.WriteLine($"  ✓ Withdrawn {amount} USD");
}

Console.WriteLine();

// Build projection
Console.WriteLine("3. Building projection (read model)...\n");
var projection = await projectionService.BuildProjectionAsync(account.Id);

Console.WriteLine($"✓ Projection built for account: {account.Id}");
Console.WriteLine($"  Current Balance: {projection.CurrentBalance} USD");
Console.WriteLine($"  Total Deposits: {projection.TotalDeposits} USD");
Console.WriteLine($"  Total Withdrawals: {projection.TotalWithdrawals} USD");
Console.WriteLine($"  Transaction Count: {projection.TransactionCount}");
Console.WriteLine($"  Account Status: {projection.Status}");
Console.WriteLine($"  Last Updated: {projection.LastUpdated:u}\n");

// Calculate statistics
Console.WriteLine("4. Calculating statistics from projection...\n");
var totalInflows = projection.TotalDeposits;
var totalOutflows = projection.TotalWithdrawals;
var netFlow = totalInflows - totalOutflows;

Console.WriteLine($"  Total Inflows: {totalInflows} USD");
Console.WriteLine($"  Total Outflows: {totalOutflows} USD");
Console.WriteLine($"  Net Flow: {netFlow} USD");
Console.WriteLine($"  Initial Balance: 2000 USD");
Console.WriteLine($"  Expected Final: {2000m + netFlow} USD");
Console.WriteLine($"  Actual Final: {projection.CurrentBalance} USD");

bool balanceCorrect = Math.Abs(projection.CurrentBalance - (2000m + netFlow)) < 0.01m;
Console.WriteLine($"  ✓ Balance verification: {(balanceCorrect ? "PASSED" : "FAILED")}\n");

// Query via projection (optimized read)
Console.WriteLine("5. Querying via projection (fast read model)...\n");
Console.WriteLine($"  Quick query: Account {account.Id} has balance {projection.CurrentBalance} USD");
Console.WriteLine($"  This is much faster than replaying all {projection.TransactionCount} events!\n");

Console.WriteLine("=== Example Complete ===");
