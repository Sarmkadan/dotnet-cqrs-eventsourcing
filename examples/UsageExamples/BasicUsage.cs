using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;

// Minimal setup and first call example
// This shows the most basic way to initialize the framework and use a service.

var services = new ServiceCollection();
// Configure the framework
services.AddCqrsFramework();

// Add in-memory repository for this example
services.AddSingleton<DotNetCqrsEventSourcing.Data.Repositories.IEventRepository, 
                      DotNetCqrsEventSourcing.Data.Repositories.InMemoryEventRepository>();

var serviceProvider = services.BuildServiceProvider();

// Resolve the account service
var accountService = serviceProvider.GetRequiredService<IAccountService>();

// Perform a basic operation
var result = await accountService.CreateAccountAsync(
    accountId: "ACC-001",
    accountHolderName: "John Doe",
    currency: "USD",
    initialBalance: 1000m
);

if (result.IsSuccess)
{
    Console.WriteLine($"Account created: {result.Data.Id} with balance {result.Data.Balance.CurrentAmount}");
}
else
{
    Console.WriteLine($"Failed to create account: {result.Error}");
}
