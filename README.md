# dotnet-cqrs-eventsourcing

CQRS + Event Sourcing framework in C# with a banking `Account` aggregate as the reference domain.

## Architecture

The full picture - layers, write/read data flow, projection engine, snapshots/compaction, extension points and known limitations - lives in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Design decisions are recorded as ADRs in [docs/adr/](docs/adr/). Short version:

- `Domain/` - aggregates, events, value objects (no infrastructure dependencies)
- `Application/` - event store, event bus, services, sagas
- `ReadModels/` - projection engine with retries, checkpointing and dead-lettering
- `Infrastructure/` - dispatch, workers, middleware, CLI
- All default stores are in‑memory; swap `IEventRepository` / `IReadModelStore<T>` for real persistence.

## AccountAggregateTests

The `AccountAggregateTests` class provides a comprehensive set of unit tests for the `Account` aggregate root, covering various scenarios such as account creation, deposit, withdrawal, and closure. These tests ensure that the `Account` class behaves correctly under different conditions.

Example usage:
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var test = new AccountAggregateTests();

        // Test creating an account with valid parameters
        test.CreateAccount_ValidParameters_RaisesAccountCreatedEvent();

        // Test depositing a positive amount
        var account = AccountAggregateTests.CreateFreshAccount();
        account.Deposit(100m, "REF-001");
        // Verify account state...

        // Test withdrawing with sufficient funds
        account.Withdraw(50m, "REF-002");
        // Verify account state...
    }
}
```

## AccountServiceTests

The `AccountServiceTests` class contains integration‑style unit tests for the `AccountService` application service. It validates that account creation, deposits, withdrawals, closures, and related error handling work correctly by exercising the service with mocked dependencies.

Example usage:
```csharp
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Tests.Application;

public class Program
{
    public static async Task Main(string[] args)
    {
        var tests = new AccountServiceTests();

        // Run a few representative test methods manually
        await tests.CreateAccountAsync_ValidParameters_ReturnsSuccessWithAccount();
        await tests.DepositAsync_ValidAccount_SavesAndPublishesEvents();
        await tests.WithdrawAsync_InsufficientFunds_ReturnsFailure();
        await tests.CloseAccountAsync_ValidAccount_SucceedsAndPublishesClosedEvent();
    }
}
```

## EventStoreCompactionServiceTests

The `EventStoreCompactionServiceTests` class provides unit tests for the `EventStoreCompactionService`, which handles event store compaction by removing old events while preserving snapshots. These tests verify compaction behavior under various scenarios including version-based compaction, snapshot-based compaction, and error handling when snapshots are missing.

Example usage:
```csharp
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Tests.Application;
using DotNetCqrsEventSourcing.Application.Services;

public class Program
{
    public static async Task Main(string[] args)
    {
        var tests = new EventStoreCompactionServiceTests();

        // Compact events to a specific version (keep events from version 4 onwards)
        var result1 = await tests.CompactToVersionAsync_RemovesEventsBeforeVersion();
        Console.WriteLine($"Compacted to version {result1.Data?.CompactedToVersion}, removed {result1.Data?.EventsRemoved} events");

        // Compact using the latest snapshot version as the compaction point
        var result2 = await tests.CompactAsync_WithSnapshot_UsesSnapshotVersion();
        Console.WriteLine($"Snapshot-based compaction removed {result2.Data?.EventsRemoved} events");

        // Attempt compaction when no snapshot exists (should fail gracefully)
        var result3 = await tests.CompactAsync_NoSnapshot_ReturnsFailure();
        if (!result3.IsSuccess) Console.WriteLine($"Compaction failed: {result3.ErrorCode}");

        // Compact multiple aggregates, skipping those without snapshots
        var result4 = await tests.CompactAllAsync_SkipsAggregatesWithoutSnapshots();
        Console.WriteLine($"Successfully compacted {result4.Data?.Count} aggregate(s)");

        // Compact to an invalid version (should return failure)
        var result5 = await tests.CompactToVersionAsync_InvalidVersion_ReturnsFailure();
        if (!result5.IsSuccess) Console.WriteLine($"Invalid version: {result5.ErrorCode}");
    }
}
```

## TestSaga

The `TestSaga` class is a minimal saga implementation used exclusively for testing purposes. It extends `SagaBase` and demonstrates core saga behavior including state transitions, event handling, and correlation management. The class tracks the number of events processed through the `HandledEvents` property, making it ideal for verifying saga lifecycle and handler logic in unit tests.

Example usage:
```csharp
using System;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Application.Sagas;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Tests.Application.Sagas;
using DotNetCqrsEventSourcing.Shared.Results;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Create a new saga instance
        var saga = new TestSaga();
        Console.WriteLine($"Initial state: {saga.State}"); // NotStarted

        // Set correlation ID for tracking
        saga.SetCorrelation("account-123");

        // Handle an event to activate the saga
        var accountCreatedEvent = new AccountCreatedEvent("agg-1", "ACC-123", "Test User", "USD", 1000m)
        {
            CorrelationId = "account-123"
        };

        saga.Handle(accountCreatedEvent);
        Console.WriteLine($"After handling event: {saga.State}, HandledEvents = {saga.HandledEvents}"); // Active, 1

        // Mark saga as completed
        saga.Finish();
        Console.WriteLine($"After completion: {saga.State}"); // Completed

        // Create saga with correlation ID in constructor
        var saga2 = new TestSaga("corr-456");
        Console.WriteLine($"Saga with correlation: {saga2.CorrelationId}"); // corr-456

        // Use with saga handler and repository
        var repository = new InMemorySagaRepository<TestSaga>();
        var handler = new TestSagaHandler(repository);

        var result = await handler.HandleAsync(accountCreatedEvent);
        if (result.IsSuccess)
        {
            Console.WriteLine("Saga persisted successfully");
        }
    }
}
```

## CqrsException

`CqrsException` is the base exception type for all CQRS‑infrastructure errors. It carries an **ErrorCode** that identifies the problem, an optional **CorrelationId** for tracing across services, and the timestamp (**OccurredAt**) when the exception was created. Specific scenarios such as missing aggregates or event‑stream conflicts are represented by the derived `AggregateNotFoundException` and `EventStreamException` types.

**Typical usage**

```csharp
using System;
using DotNetCqrsEventSourcing.Shared.Exceptions;

public class Example
{
    public void Run()
    {
        try
        {
            // Simulate a failure that should be reported as a CQRS error
            throw new CqrsException(
                message: "Unable to process command",
                errorCode: "COMMAND_PROCESSING_FAILED",
                correlationId: "corr-42");
        }
        catch (CqrsException ex)
        {
            Console.WriteLine($"ErrorCode: {ex.ErrorCode}");
            Console.WriteLine($"CorrelationId: {ex.CorrelationId}");
            Console.WriteLine($"OccurredAt (UTC): {ex.OccurredAt:u}");
        }

        // Example of a more specific exception
        throw new AggregateNotFoundException(aggregateId: "agg-123", aggregateType: nameof(Account));

        // Example of an event‑stream concurrency exception
        var inner = new InvalidOperationException("Version mismatch");
        throw new EventStreamException("Optimistic concurrency failure", inner);
    }
}
```

The example demonstrates the public members of `CqrsException` and its derived types without relying on any hidden implementation details.

## DotnetCqrsEventsourcingException

`DotnetCqrsEventsourcingException` is the base exception type for all custom exceptions in the DotNetCqrsEventSourcing framework. It provides structured error handling with an **ErrorCode** for categorizing exceptions, an **OccurredAt** timestamp for tracking when the exception occurred, and a custom **ToString()** implementation that includes the error code.

This exception is designed to be inherited by more specific exception types throughout the framework, allowing for consistent error handling patterns.

**Public members:**
- `ErrorCode` - Gets the error code identifying the type/category of error
- `OccurredAt` - Gets the UTC timestamp when the exception was created
- `DotnetCqrsEventsourcingException(string message, string errorCode)` - Constructor with message and error code
- `DotnetCqrsEventsourcingException(string message, string errorCode, Exception? innerException)` - Constructor with message, error code, and inner exception
- `ToString()` - Override that includes the error code in the output

**Typical usage**

```csharp
using System;
using DotNetCqrsEventSourcing.Shared.Exceptions;

public class Example
{
    public void ProcessAccountCommand(AccountCommand command)
    {
        try
        {
            // Business logic that might throw domain-specific exceptions
            ProcessCommandInternal(command);
        }
        catch (DotnetCqrsEventsourcingException ex)
        {
            // Handle framework-specific exceptions
            Console.WriteLine($"Error occurred at {ex.OccurredAt:u}");
            Console.WriteLine($"Error code: {ex.ErrorCode}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Full string representation: {ex}");

            // Log or propagate the error
            LogError(ex);
        }
    }

    private void ProcessCommandInternal(AccountCommand command)
    {
        // Simulate a domain error
        throw new DotnetCqrsEventsourcingException(
            message: "Account operation failed due to validation rules",
            errorCode: "ACCOUNT_VALIDATION_FAILED");
    }

    private void LogError(DotnetCqrsEventsourcingException ex)
    {
        // Example of logging with all available information
        var logMessage = $"[{ex.OccurredAt:u}] [{ex.ErrorCode}] {ex.Message}";
        Console.WriteLine(logMessage);

        // The ToString() method provides a formatted output
        Console.WriteLine($"Exception details: {ex}");
    }
}
```

The example demonstrates all public members of `DotnetCqrsEventsourcingException` with realistic usage patterns for error handling and logging.

## ValidationException

`ValidationException` is thrown when input validation fails during command processing or domain validation. It collects validation errors in a `Dictionary<string, string>` where the key is the field name and the value is the error message. This exception is commonly used for validating command parameters, DTOs, and domain entity state before processing operations.

The exception provides several factory methods for common validation scenarios:
- `InvalidInput` - for validating user input or command parameters
- `InvalidArgument` - for validating method arguments
- `AggregateValidationFailed` - for validating aggregate state before applying commands

**Typical usage**

```csharp
using System;
using System.Collections.Generic;
using DotNetCqrsEventSourcing.Shared.Exceptions;

public class Example
{
    public void ValidateCreateAccountCommand(CreateAccountCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserName))
            throw ValidationException.InvalidInput(nameof(command.UserName), "User name is required");

        if (command.InitialBalance < 0)
            throw ValidationException.InvalidInput(nameof(command.InitialBalance), "Initial balance cannot be negative");

        if (string.IsNullOrWhiteSpace(command.Currency))
            throw ValidationException.InvalidInput(nameof(command.Currency), "Currency is required");
    }

    public void ValidateAccountState(Account account)
    {
        if (account.IsClosed)
            throw ValidationException.AggregateValidationFailed(
                nameof(Account), 
                account.Id, 
                "Cannot perform operations on a closed account");

        if (account.Balance < 0)
            throw ValidationException.AggregateValidationFailed(
                nameof(Account), 
                account.Id, 
                "Account balance cannot be negative");
    }

    public void ManualValidationErrors()
    {
        var exception = new ValidationException("Multiple validation errors occurred");
        exception.WithError("Email", "Email is not valid");
        exception.WithError("Password", "Password must be at least 8 characters");
        exception.WithError("ConfirmPassword", "Passwords do not match");

        // Access all validation errors
        foreach (var error in exception.ValidationErrors)
        {
            Console.WriteLine($"{error.Key}: {error.Value}");
        }
    }
}
```

The example demonstrates all public members of `ValidationException`: the `ValidationErrors` dictionary, the base constructors, the `WithError` method, and the factory methods.

## ConfigurationException

`ConfigurationException` is thrown when there are errors in application configuration or validation. It extends `DotnetCqrsEventsourcingException` and provides factory methods for common configuration error scenarios such as missing required configuration values, invalid configuration values, and configuration validation failures.

**Public members:**
- Constructors for creating custom configuration exceptions
- `MissingRequiredConfiguration(string configurationKey)` - factory method for missing configuration
- `InvalidConfigurationValue(string configurationKey, string value)` - factory method for invalid configuration values
- `ValidationFailed(string validationMessage)` - factory method for configuration validation failures

**Typical usage**

```csharp
using System;
using DotNetCqrsEventSourcing.Shared.Exceptions;

public class ConfigurationExample
{
    public void LoadConfiguration(string[] args)
    {
        // Example 1: Missing required configuration
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw ConfigurationException.MissingRequiredConfiguration("API_KEY");

        // Example 2: Invalid configuration value
        var timeoutValue = Environment.GetEnvironmentVariable("TIMEOUT_SECONDS");
        if (!int.TryParse(timeoutValue, out var timeout) || timeout <= 0)
            throw ConfigurationException.InvalidConfigurationValue("TIMEOUT_SECONDS", timeoutValue ?? "null");

        // Example 3: Configuration validation failed
        var maxConnections = Environment.GetEnvironmentVariable("MAX_CONNECTIONS");
        if (string.IsNullOrEmpty(maxConnections) || !int.TryParse(maxConnections, out var max) || max < 1 || max > 100)
            throw ConfigurationException.ValidationFailed("MAX_CONNECTIONS must be a number between 1 and 100");

        // Example 4: Custom configuration exception
        try
        {
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (string.IsNullOrEmpty(databaseUrl))
                throw new ConfigurationException(
                    "Database connection string is required", 
                    "DB_CONNECTION_MISSING");
        }
        catch (ConfigurationException ex)
        {
            Console.WriteLine($"Configuration error occurred at {ex.OccurredAt:u}");
            Console.WriteLine($"Error code: {ex.ErrorCode}");
            Console.WriteLine($"Message: {ex.Message}");
        }
    }
}
```

The example demonstrates all public members of `ConfigurationException` including the factory methods and custom exception creation with proper error handling.

## TransactionSummary

`TransactionSummary` is an immutable record that represents a single credit or debit transaction entry recorded against an account. It is appended to the `Transactions` collection of an `AccountReadModel` by the `AccountProjector` whenever a `MoneyDeposited` or `MoneyWithdrawn` event is projected.

Each transaction captures the essential details of a financial movement including the event identifier, transaction type (deposit or withdrawal), amount, currency, reference text, and processing timestamp.

**Public members:**
- `EventId` - Identifier of the domain event that produced this transaction entry
- `Type` - Direction of the movement: "Deposit" or "Withdrawal"
- `Amount` - Absolute monetary amount of the transaction (always positive)
- `Currency` - ISO 4217 currency code inherited from the account (e.g., USD)
- `Reference` - Human-readable reference text supplied at the time of the transaction
- `ProcessedAt` - UTC timestamp when the transaction was processed on the command side

**Typical usage**

```csharp
using System;
using System.Linq;
using DotNetCqrsEventSourcing.ReadModels;

public class TransactionExample
{
    public void ProcessAccountTransactions(AccountReadModel account)
    {
        // Access the transaction history
        Console.WriteLine($"Account {account.AccountNumber} has {account.TransactionCount} transactions");
        
        // Iterate through transactions
        foreach (var transaction in account.Transactions.OrderBy(t => t.ProcessedAt))
        {
            var direction = transaction.Type;
            var amount = transaction.Amount;
            var currency = transaction.Currency;
            var reference = transaction.Reference;
            var processedAt = transaction.ProcessedAt;
            
            Console.WriteLine($"[{processedAt:yyyy-MM-dd HH:mm:ss}] {direction}: {amount} {currency} - {reference}");
        }
        
        // Filter deposits only
        var deposits = account.Transactions
            .Where(t => t.Type == "Deposit")
            .Sum(t => t.Amount);
        
        Console.WriteLine($"Total deposits: {deposits}");
        
        // Filter withdrawals only
        var withdrawals = account.Transactions
            .Where(t => t.Type == "Withdrawal")
            .Sum(t => t.Amount);
        
        Console.WriteLine($"Total withdrawals: {withdrawals}");
        
        // Create a new transaction summary (typically done by the projector)
        var transaction = new TransactionSummary(
            EventId: Guid.NewGuid().ToString(),
            Type: "Deposit",
            Amount: 1000.50m,
            Currency: "USD",
            Reference: "Salary payment",
            ProcessedAt: DateTime.UtcNow
        );
        
        Console.WriteLine($"Created transaction: {transaction.Type} {transaction.Amount} {transaction.Currency}");
    }
}
```

The example demonstrates all public members of `TransactionSummary` with realistic usage patterns for querying and processing account transactions.


## IAccountReadModelQueryService

`IAccountReadModelQueryService` provides domain-oriented queries over the `AccountReadModel` store, hiding low-level key lookups behind business-meaningful method signatures. It serves as the primary read-side interface for querying account data in the CQRS + Event Sourcing framework.

All methods return `Result<T>` so callers can distinguish between genuine "not found" scenarios and infrastructure failures without relying on exceptions.

Example usage:

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.ReadModels;
using DotNetCqrsEventSourcing.Shared.Results;

public class AccountQueryServiceExample
{
    private readonly IAccountReadModelQueryService _queryService;

    public AccountQueryServiceExample(IAccountReadModelQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task QueryAccountsAsync()
    {
        // Get account by aggregate ID
        var accountById = await _queryService.GetByIdAsync("agg-123");
        if (accountById.IsSuccess)
        {
            Console.WriteLine($"Found account: {accountById.Data?.AccountNumber}");
        }

        // Get account by account number
        var accountByNumber = await _queryService.GetByAccountNumberAsync("ACC-0001");
        if (accountByNumber.IsSuccess)
        {
            Console.WriteLine($"Account holder: {accountByNumber.Data?.AccountHolder}");
        }

        // Get all active accounts
        var activeAccounts = await _queryService.GetActiveAccountsAsync();
        if (activeAccounts.IsSuccess)
        {
            Console.WriteLine($"Active accounts: {activeAccounts.Data?.Count}");
        }

        // Find accounts by account holder name (substring match)
        var holderAccounts = await _queryService.GetByAccountHolderAsync("John");
        if (holderAccounts.IsSuccess)
        {
            foreach (var account in holderAccounts.Data ?? Enumerable.Empty<AccountReadModel>())
            {
                Console.WriteLine($"Account: {account.AccountNumber}, Balance: {account.CurrentBalance}");
            }
        }

        // Get top N accounts by balance
        var topAccounts = await _queryService.GetTopBalanceAccountsAsync(10);
        if (topAccounts.IsSuccess)
        {
            var top10 = topAccounts.Data ?? Enumerable.Empty<AccountReadModel>();
            Console.WriteLine($"Top {top10.Count()} accounts by balance:");
        }

        // Get accounts within balance range
        var rangeAccounts = await _queryService.GetByBalanceRangeAsync(1000, 10000);
        if (rangeAccounts.IsSuccess)
        {
            Console.WriteLine($"Accounts with balance between 1000 and 10000: {rangeAccounts.Data?.Count}");
        }

        // Get portfolio statistics
        var statsResult = await _queryService.GetPortfolioStatisticsAsync();
        if (statsResult.IsSuccess)
        {
            var stats = statsResult.Data!;
            Console.WriteLine($"Portfolio: {stats.TotalAccounts} total, {stats.ActiveAccounts} active");
            Console.WriteLine($"Total balance: {stats.TotalActiveBalance}, Average: {stats.AverageActiveBalance}");
        }

        // Convenience methods (non-Result returning)
        var account = await _queryService.GetAccountByIdAsync("agg-456");
        var allAccounts = await _queryService.GetAllAccountsAsync();
    }
}
```

## ReadModelProjectionEngine

`ReadModelProjectionEngine` orchestrates eventually consistent read-model projections by subscribing to the application event bus and routing domain events to registered projection runners. It provides configurable retry with exponential back-off, per-projection checkpointing, bounded concurrency, and on-demand aggregate replay for rebuilds.

Example usage:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.ReadModels;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ReadModelProjectionEngineExample
{
    public async Task RunProjectionEngineExample(
        IEventBus eventBus,
        IEventStore eventStore,
        IEnumerable<IReadModelProjectionRunner> runners,
        IOptions<ReadModelProjectionOptions> options,
        ILogger<ReadModelProjectionEngine> logger,
        IDeadLetterStore? deadLetterStore = null,
        CancellationToken cancellationToken = default)
    {
        // Create the projection engine
        using var projectionEngine = new ReadModelProjectionEngine(
            eventBus,
            eventStore,
            runners,
            options,
            logger,
            deadLetterStore);

        // Rebuild projections for a specific aggregate
        string aggregateId = "agg-123";
        Result<ProjectionRebuildResult> rebuildResult = await projectionEngine.RebuildAsync(
            aggregateId, 
            cancellationToken);

        if (rebuildResult.IsSuccess)
        {
            ProjectionRebuildResult result = rebuildResult.Data;
            Console.WriteLine($"Rebuilt projections for aggregate {result.AggregateId}: " +
                $"{result.TotalEventsReplayed} events replayed, {result.FailedEventIds.Count} failures");
        }
        else
        {
            Console.WriteLine($"Rebuild failed: {rebuildResult.ErrorMessage}");
        }

        // Rebuild projections for multiple aggregates
        List<string> aggregateIds = new() { "agg-123", "agg-456", "agg-789" };
        Result<IReadOnlyList<ProjectionRebuildResult>> rebuildAllResult = await projectionEngine.RebuildAllAsync(
            aggregateIds,
            cancellationToken);

        if (rebuildAllResult.IsSuccess)
        {
            IReadOnlyList<ProjectionRebuildResult> results = rebuildAllResult.Data;
            Console.WriteLine($"Rebuilt projections for {results.Count} aggregates");
            
            foreach (var result in results)
            {
                Console.WriteLine($"  Aggregate {result.AggregateId}: " +
                    $"{result.TotalEventsReplayed} events replayed");
            }
        }
        else
        {
            Console.WriteLine($"Rebuild all failed: {rebuildAllResult.ErrorMessage}");
        }

        // Get checkpoint for a specific projection
        string projectionName = "AccountReadModel";
        ProjectionCheckpoint? checkpoint = projectionEngine.GetCheckpoint(projectionName);
        if (checkpoint != null)
        {
            Console.WriteLine($"Checkpoint for {projectionName}: " +
                $"EventId={checkpoint.EventId}, Version={checkpoint.AggregateVersion}, " +
                $"Timestamp={checkpoint.Timestamp:u}, ProcessedEvents={checkpoint.ProcessedEvents}");
        }
        else
        {
            Console.WriteLine($"No checkpoint found for projection {projectionName}");
        }

        // The engine will be disposed automatically when exiting the using block,
        // which unsubscribes from the event bus and releases resources
    }
}
```

## InMemoryReadModelStore

`InMemoryReadModelStore<TReadModel>` is a thread-safe, in-process implementation of `IReadModelStore<TReadModel>` backed by a dictionary protected with a monitor lock. It's designed for development, testing, and single-instance deployments where persistence requirements are minimal. For distributed scenarios or production workloads, replace this with a database-backed implementation.

The store provides basic CRUD operations with atomic updates and supports querying via predicates. A diagnostic method `GetAllKeys()` returns all keys for testing and observability purposes.

Example usage:

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.ReadModels;
using DotNetCqrsEventSourcing.Shared.Results;

public class AccountReadModelStoreExample
{
    private readonly InMemoryReadModelStore<AccountReadModel> _store = 
        new InMemoryReadModelStore<AccountReadModel>();

    public async Task ManageAccountReadModels()
    {
        // Create a new account read model
        var account = new AccountReadModel
        {
            AggregateId = "agg-123",
            AccountNumber = "ACC-0001",
            AccountHolder = "John Doe",
            CurrentBalance = 1000.00m,
            Currency = "USD",
            IsClosed = false
        };

        // Upsert: Add or update a read model
        var upsertResult = await _store.UpsertAsync(account.AggregateId, account);
        if (upsertResult.IsSuccess)
        {
            Console.WriteLine("Account read model upserted successfully");
        }

        // Get a single read model by key
        var getResult = await _store.GetAsync("agg-123");
        if (getResult.IsSuccess)
        {
            var retrievedAccount = getResult.Data;
            Console.WriteLine($"Retrieved account: {retrievedAccount.AccountNumber}, Balance: {retrievedAccount.CurrentBalance}");
        }

        // Query: Find all accounts with balance > 500
        var queryResult = await _store.QueryAsync(acc => acc.CurrentBalance > 500);
        if (queryResult.IsSuccess)
        {
            var highBalanceAccounts = queryResult.Data;
            Console.WriteLine($"Found {highBalanceAccounts.Count} accounts with balance > 500");
        }

        // Get all read models
        var allResult = await _store.GetAllAsync();
        if (allResult.IsSuccess)
        {
            var allAccounts = allResult.Data;
            Console.WriteLine($"Total accounts in store: {allAccounts.Count}");
        }

        // Get count of read models
        var countResult = await _store.GetCountAsync();
        if (countResult.IsSuccess)
        {
            Console.WriteLine($"Current count: {countResult.Data}");
        }

        // Delete a read model
        var deleteResult = await _store.DeleteAsync("agg-123");
        if (deleteResult.IsSuccess)
        {
            Console.WriteLine("Account deleted successfully");
        }

        // Clear all read models
        await _store.ClearAsync();
        Console.WriteLine("Store cleared");

        // Diagnostic: Get all keys (for testing/observability)
        var keys = _store.GetAllKeys();
        Console.WriteLine($"Current keys: {string.Join(", ", keys)}");
    }
}
```

## ValidationExtensions

`ValidationExtensions` provides a comprehensive set of guard clause extension methods for validating method parameters and business rules. These extensions follow a fluent, exception-throwing pattern that integrates seamlessly with C# method chaining, making validation code more readable and maintainable.

The extension methods cover common validation scenarios including null checks, string validation, numeric range validation, collection validation, and domain-specific validations like email format and GUID validation.

**Typical usage**

```csharp
using System;
using System.Collections.Generic;
using DotNetCqrsEventSourcing.Shared.Extensions;

public class AccountService
{
    public void CreateAccount(string userName, decimal initialBalance, string currency, string email)
    {
        // Fluent validation with exception throwing
        userName = userName.NotNullOrEmpty(nameof(userName));
        initialBalance = initialBalance.NotNegative(nameof(initialBalance));
        currency = currency.NotNullOrEmpty(nameof(currency)).MaxLength(3, nameof(currency));
        email = email.ValidEmail(nameof(email));

        // Alternative: validation with custom error messages
        var accountId = Guid.NewGuid().ToString();
        accountId = accountId.ValidGuid(nameof(accountId));

        // Collection validation
        var tags = new List<string> { "premium", "active" };
        tags = tags.NotEmpty(nameof(tags));

        // Numeric range validation
        var transferAmount = 100.50m;
        transferAmount = transferAmount.InRange(0.01m, 10000.00m, nameof(transferAmount));

        // Domain validation
        var isValid = true;
        isValid.Ensure("Operation is not allowed in current state", "ACCOUNT_INVALID_STATE");
    }

    public (bool IsValid, string? ErrorMessage) ValidateAccountInput(string? userName, decimal balance)
    {
        // Validation with result tuples instead of throwing
        var nameResult = userName.ValidateRequired(nameof(userName));
        if (!nameResult.IsValid) return (false, nameResult.ErrorMessage);

        var balanceResult = balance.ValidateRange(0, 1000000, nameof(balance));
        if (!balanceResult.IsValid) return (false, balanceResult.ErrorMessage);

        return (true, null);
    }
}
```

The example demonstrates the key extension methods:
- `NotNull<T>` - validates null references
- `NotNullOrEmpty` - validates strings are not null or empty
- `NotNegative` - validates decimal values are non-negative
- `InRange` - validates values fall within specified bounds
- `ValidGuid` - validates string format as GUID
- `ValidEmail` - validates email format using System.Net.Mail
- `NotEmpty<T>` - validates collections are not empty
- `MaxLength` / `MinLength` - validates string length constraints
- `Ensure` - validates boolean conditions with domain exceptions
- `ValidateRequired` / `ValidateRange` - validation with result tuples instead of exceptions

## EventStoreBenchmarks

The `EventStoreBenchmarks` class provides comprehensive performance benchmarks for the CQRS + Event Sourcing framework, measuring throughput, latency, and memory allocations for critical operations. These benchmarks help identify performance bottlenecks in event store operations, aggregate replay performance, and service layer operations under realistic workloads.

The benchmarks cover:
- Event append operations (single events and batches of 100 events)
- Aggregate root replay performance with varying event counts (100, 1,000, 10,000 events)
- Event retrieval operations (by aggregate ID, from specific version, version lookup)
- Complete account lifecycle scenarios including account creation and transaction processing


Example usage:

```csharp
using BenchmarkDotNet.Running;
using dotnet_cqrs_eventsourcing.Benchmarks.Benchmarks;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Create benchmark instance and initialize
        var benchmarks = new EventStoreBenchmarks();
        benchmarks.GlobalSetup();
        
        // Test event append throughput
        await benchmarks.EventStore_AppendSingleEvent();
        await benchmarks.EventStore_AppendBatchOf100Events();
        
        // Test aggregate replay performance
        benchmarks.AggregateRoot_Replay100Events();
        benchmarks.AggregateRoot_Replay1000Events();
        benchmarks.AggregateRoot_Replay10000Events();
        
        // Test event retrieval operations
        await benchmarks.EventStore_GetEventsByAggregateId();
        await benchmarks.EventStore_GetEventsFromVersion();
        await benchmarks.EventStore_GetAggregateVersion();
        
        // Test account service operations
        await benchmarks.AccountService_CreateAccount();
        await benchmarks.AccountService_CompleteLifecycle();
        
        // Cleanup
        benchmarks.GlobalCleanup();
        
        Console.WriteLine("All benchmarks completed successfully!");
    }
}
```

## Result

`Result<T>` is a generic result type that represents either a successful operation with a value or a failure with error information. It provides a functional programming approach to error handling, avoiding exceptions for expected error cases and making error handling more explicit and composable.

The type is commonly used throughout the framework for command handlers, repository operations, and service methods to return operation results with proper error information.

**Public members:**
- `IsSuccess` - Gets whether the result represents a successful operation
- `Data` - Gets the successful value (null when IsSuccess is false)
- `ErrorCode` - Gets the error code when the operation failed (null when successful)
- `ErrorMessage` - Gets the error message when the operation failed (null when successful)
- `Error` - Convenience property alias for ErrorMessage
- `Errors` - Gets the collection of error messages for the failure
- `Success(T data)` - Factory method to create a successful result with data
- `Failure(string errorCode, string errorMessage)` - Factory method to create a failed result
- `Failure(string errorCode, string errorMessage, List<string> errors)` - Factory method to create a failed result with multiple errors
- `AddError(string error)` - Adds an additional error message to the result
- `Match<TOut>(Func<T, TOut> onSuccess, Func<string?, TOut> onFailure)` - Pattern matching for handling both success and failure cases
- `Match(Action<T> onSuccess, Action<string?> onFailure)` - Pattern matching for handling both success and failure cases without returning a value
- `ThrowIfFailure()` - Throws an exception if the result represents a failure
- `MapSuccess<TOut>(Func<T, TOut> transform)` - Transforms the successful value while preserving failure state
- `ToString()` - Returns a string representation of the result

**Typical usage**

```csharp
using System;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Shared.Results;

public class AccountService
{
public async Task<Result<Account>> GetAccountAsync(string accountId)
{
// Example: Successful retrieval
var account = await _repository.GetByIdAsync(accountId);
if (account != null)
return Result<Account>.Success(account);

// Example: Failed retrieval
return Result<Account>.Failure("ACCOUNT_NOT_FOUND", "Account not found");
}

public Result<decimal> CalculateTransferFee(decimal amount)
{
if (amount <= 0)
return Result<decimal>.Failure("INVALID_AMOUNT", "Transfer amount must be positive");

if (amount > 10000)
return Result<decimal>.Failure("AMOUNT_TOO_LARGE", "Transfer amount exceeds maximum limit",
new List<string> { "Maximum allowed transfer amount is 10000", "Please contact support for larger transfers" });

return Result<decimal>.Success(amount * 0.01m); // 1% fee
}

public void ProcessAccountCommand(AccountCommand command)
{
// Using Match for handling both success and failure
var result = ValidateCommand(command);
result.Match(
onSuccess: account => ProcessAccount(account, command),
// Using pattern matching with error information
onFailure: errorMessage => LogError(result.ErrorCode, result.Errors));

// Using ThrowIfFailure to convert result to exception
var feeResult = CalculateTransferFee(command.Amount);
feeResult.ThrowIfFailure();
var fee = feeResult.Data;

// Using MapSuccess to transform successful results
var accountResult = _repository.GetByIdAsync(command.AccountId);
var dtoResult = accountResult.MapSuccess(account => new AccountDto(account));
if (dtoResult.IsSuccess)
{
Console.WriteLine($"Account DTO created: {dtoResult.Data}");
}
}

private Result<Account> ValidateCommand(AccountCommand command)
{
if (string.IsNullOrEmpty(command.AccountId))
return Result<Account>.Failure("INVALID_ACCOUNT_ID", "Account ID is required");

if (command.Amount <= 0)
return Result<Account>.Failure("INVALID_AMOUNT", "Amount must be positive");

// Success case
return Result<Account>.Success(new Account(command.AccountId));
}

private void ProcessAccount(Account account, AccountCommand command)
{
// Business logic for processing
account.Deposit(command.Amount, command.Reference);
}

private void LogError(string? errorCode, List<string> errors)
{
Console.WriteLine($"Error [{errorCode}]: {string.Join(", ", errors)}");
}
}
```