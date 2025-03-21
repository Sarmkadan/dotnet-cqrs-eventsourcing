// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Shared.Exceptions;

Console.WriteLine("=== Error Handling Example ===\n");

// Setup dependency injection
var services = new ServiceCollection();
services.AddCqrsFramework();
var serviceProvider = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<IAccountService>();

// Create account for testing
Console.WriteLine("1. Creating test account...\n");
var createResult = await accountService.CreateAccountAsync(
    "ACC-ERROR-001",
    "Eve Martinez",
    "USD",
    500m
);

if (!createResult.IsSuccess)
{
    Console.WriteLine($"Error creating account: {createResult.Error}");
    return;
}

Console.WriteLine($"✓ Account created with balance: 500 USD\n");

// Test case 1: Insufficient funds
Console.WriteLine("2. Testing insufficient funds error...\n");
var withdrawResult = await accountService.WithdrawAsync(
    "ACC-ERROR-001",
    1000m, // More than available balance
    "WTH-001"
);

if (!withdrawResult.IsSuccess)
{
    Console.WriteLine($"⚠ Withdrawal failed (expected):");
    Console.WriteLine($"  Error: {withdrawResult.Error}");
    Console.WriteLine($"  This prevents invalid operations\n");
}

// Test case 2: Invalid amount
Console.WriteLine("3. Testing invalid amount error...\n");
var invalidResult = await accountService.WithdrawAsync(
    "ACC-ERROR-001",
    -100m, // Negative amount
    "WTH-002"
);

if (!invalidResult.IsSuccess)
{
    Console.WriteLine($"⚠ Withdrawal failed (expected):");
    Console.WriteLine($"  Error: {invalidResult.Error}");
    Console.WriteLine($"  Domain validation prevents invalid operations\n");
}

// Test case 3: Invalid account
Console.WriteLine("4. Testing non-existent account error...\n");
var notFoundResult = await accountService.GetAccountAsync("ACC-NONEXISTENT");

if (!notFoundResult.IsSuccess)
{
    Console.WriteLine($"⚠ Account lookup failed (expected):");
    Console.WriteLine($"  Error: {notFoundResult.Error}\n");
}

// Test case 4: Successful operation
Console.WriteLine("5. Testing successful operation...\n");
var successResult = await accountService.WithdrawAsync(
    "ACC-ERROR-001",
    200m, // Valid amount
    "WTH-003"
);

if (successResult.IsSuccess)
{
    Console.WriteLine($"✓ Withdrawal successful:");
    Console.WriteLine($"  New balance: {successResult.Data} USD\n");
}

// Test case 5: Handling domain exceptions
Console.WriteLine("6. Demonstrating exception handling...\n");

try
{
    var account = await accountService.GetAccountAsync("ACC-ERROR-001");
    if (!account.IsSuccess)
    {
        throw new DomainException($"Account not found: {account.Error}");
    }
}
catch (DomainException ex)
{
    Console.WriteLine($"✓ Caught domain exception:");
    Console.WriteLine($"  Message: {ex.Message}\n");
}

// Test case 6: Result pattern benefits
Console.WriteLine("7. Result pattern benefits...\n");
Console.WriteLine("  ✓ No exceptions thrown on business rule violations");
Console.WriteLine("  ✓ Explicit error handling with Result<T>");
Console.WriteLine("  ✓ Strongly-typed success and failure cases");
Console.WriteLine("  ✓ Clear separation of happy path and error handling");
Console.WriteLine("  ✓ No stack unwinding overhead for validation failures\n");

// Test case 7: Complex error scenarios
Console.WriteLine("8. Testing complex scenarios...\n");

var scenario1 = await accountService.DepositAsync("ACC-ERROR-001", 100m, "DEP-001");
Console.WriteLine($"Deposit 100: {(scenario1.IsSuccess ? "✓ Success" : $"✗ {scenario1.Error}")}");

var scenario2 = await accountService.WithdrawAsync("ACC-ERROR-001", 250m, "WTH-004");
Console.WriteLine($"Withdraw 250 (balance 600): {(scenario2.IsSuccess ? "✓ Success" : $"✗ {scenario2.Error}")}");

var scenario3 = await accountService.WithdrawAsync("ACC-ERROR-001", 400m, "WTH-005");
Console.WriteLine($"Withdraw 400 (balance 350): {(scenario3.IsSuccess ? "✓ Success" : $"✗ {scenario3.Error}")}\n");

// Final state
var final = await accountService.GetAccountAsync("ACC-ERROR-001");
if (final.IsSuccess)
{
    Console.WriteLine($"Final account balance: {final.Data.Balance.CurrentAmount} USD");
}

Console.WriteLine("\n=== Example Complete ===");
