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