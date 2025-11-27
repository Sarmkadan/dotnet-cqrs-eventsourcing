// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Configuration;

// Build service provider
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Register CQRS framework
services.AddCqrsFramework();

var serviceProvider = services.BuildServiceProvider();

// Configure event handlers
serviceProvider.ConfigureEventHandlers();

// Get logger and services
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var accountService = serviceProvider.GetRequiredService<IAccountService>();
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();
var snapshotService = serviceProvider.GetRequiredService<ISnapshotService>();

logger.LogInformation("Starting CQRS + Event Sourcing Framework Demo");
logger.LogInformation("=".PadRight(50, '='));

try
{
    // 1. Create a new account
    logger.LogInformation("1. Creating new account...");
    var createResult = await accountService.CreateAccountAsync(
        "ACC-001",
        "John Doe",
        "USD",
        1000m
    );

    if (!createResult.IsSuccess)
    {
        logger.LogError("Failed to create account: {Error}", createResult.ErrorMessage);
        return;
    }

    var account = createResult.Data!;
    logger.LogInformation("Account created: {Account}", account);

    // 2. Deposit funds
    logger.LogInformation("\n2. Depositing funds...");
    var depositResult = await accountService.DepositAsync(account.Id, 500m, "Initial deposit");
    if (depositResult.IsSuccess)
        logger.LogInformation("Deposit successful");

    // 3. Withdraw funds
    logger.LogInformation("\n3. Withdrawing funds...");
    var withdrawResult = await accountService.WithdrawAsync(account.Id, 200m, "Withdrawal");
    if (withdrawResult.IsSuccess)
        logger.LogInformation("Withdrawal successful");

    // 4. Get updated account
    logger.LogInformation("\n4. Retrieving updated account...");
    var getResult = await accountService.GetAccountAsync(account.Id);
    if (getResult.IsSuccess)
    {
        var updatedAccount = getResult.Data!;
        logger.LogInformation("Updated Account: {Account}", updatedAccount);
        logger.LogInformation("Current Balance: {Balance}", updatedAccount.Balance.CurrentAmount);
        logger.LogInformation("Transaction Count: {TransactionCount}", updatedAccount.Transactions.Count);
    }

    // 5. Get event stream
    logger.LogInformation("\n5. Event Stream:");
    var eventsResult = await eventStore.GetEventStreamAsync(account.Id);
    if (eventsResult.IsSuccess)
    {
        logger.LogInformation("Total events: {EventCount}", eventsResult.Data!.Count);
        foreach (var evt in eventsResult.Data!)
        {
            logger.LogInformation("  - {EventType} (v{Version}): {Event}", evt.GetEventType(), evt.AggregateVersion, evt);
        }
    }

    // 6. Get projection
    logger.LogInformation("\n6. Projection (Read Model):");
    var projectionResult = await projectionService.GetProjectionAsync(account.Id);
    if (projectionResult.IsSuccess)
    {
        var projection = projectionResult.Data!;
        logger.LogInformation("Projection data:");
        foreach (var kvp in projection)
        {
            logger.LogInformation("  {Key}: {Value}", kvp.Key, kvp.Value);
        }
    }

    // 7. Create snapshot
    logger.LogInformation("\n7. Creating snapshot...");
    var snapshotData = System.Text.Json.JsonSerializer.Serialize(account);
    var snapshotResult = await snapshotService.CreateSnapshotAsync(account.Id, account.Version, snapshotData);
    if (snapshotResult.IsSuccess)
    {
        logger.LogInformation("Snapshot created for version {Version}", account.Version);
    }

    // 8. Retrieve snapshot
    logger.LogInformation("\n8. Retrieving snapshot...");
    var getSnapshotResult = await snapshotService.GetLatestSnapshotAsync(account.Id);
    if (getSnapshotResult.IsSuccess)
    {
        logger.LogInformation("Retrieved snapshot at version {Version}", getSnapshotResult.Data!.Version);
    }

    // 9. Get all accounts
    logger.LogInformation("\n9. Retrieving all accounts...");
    var allAccountsResult = await accountService.GetAllAccountsAsync();
    if (allAccountsResult.IsSuccess)
    {
        logger.LogInformation("Total accounts: {Count}", allAccountsResult.Data!.Count);
        foreach (var acc in allAccountsResult.Data!)
        {
            logger.LogInformation("  - {Account}", acc);
        }
    }

    // 10. Close account
    logger.LogInformation("\n10. Closing account...");
    var closeResult = await accountService.CloseAccountAsync(account.Id, "Account closure requested by customer");
    if (closeResult.IsSuccess)
    {
        logger.LogInformation("Account closed successfully");
    }

    // 11. Verify closed account
    logger.LogInformation("\n11. Verifying closed account...");
    var finalResult = await accountService.GetAccountAsync(account.Id);
    if (finalResult.IsSuccess)
    {
        logger.LogInformation("Final Account Status: {Status}", finalResult.Data!.Status);
        logger.LogInformation("Final Balance: {Balance}", finalResult.Data!.Balance.CurrentAmount);
    }

    logger.LogInformation("\n" + "=".PadRight(50, '='));
    logger.LogInformation("Demo completed successfully!");
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred during demonstration");
}
