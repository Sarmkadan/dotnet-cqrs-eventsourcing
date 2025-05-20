using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Data.Repositories;

// Advanced usage example demonstrating configuration and error handling.
// This example sets up logging, uses a specific repository, and demonstrates robust result handling.

var services = new ServiceCollection();

// 1. Configure logging
services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Debug);
});

// 2. Register Framework
services.AddCqrsFramework();

// 3. Register custom repository
services.AddSingleton<IEventRepository, InMemoryEventRepository>();

var serviceProvider = services.BuildServiceProvider();
var accountService = serviceProvider.GetRequiredService<IAccountService>();

// 4. Perform operation with explicit error handling
var result = await accountService.WithdrawAsync(
    accountId: "ACC-001",
    amount: 50000m, // Excessive amount to trigger error
    referenceNumber: "WTH-ERR-001"
);

if (result.IsSuccess)
{
    Console.WriteLine($"Withdrawal success! New balance: {result.Data}");
}
else
{
    // Handle specific errors based on business logic
    switch (result.Error)
    {
        case "InsufficientFunds":
            Console.WriteLine("Error: Cannot withdraw more than available balance.");
            break;
        default:
            Console.WriteLine($"An error occurred: {result.Error}");
            break;
    }
}
