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

## SagaBase

`SagaBase` is the abstract base class for all saga implementations in the CQRS + Event Sourcing framework. It provides lifecycle management, outbox event collection, state transitions, and correlation tracking for implementing distributed sagas that coordinate multiple domain operations across aggregates and services.

Sagas extend `SagaBase` to implement long-running business processes that need to track their state across multiple events and potentially trigger compensating actions if failures occur. The base class manages the saga's unique identifier, timestamps, and state machine transitions.

**Public members:**
- `SagaId` - Gets the unique identifier for this saga instance
- `SagaName` - Gets the name of the saga (must be implemented by derived classes)
- `State` - Gets the current saga state (NotStarted, Active, Completed, Compensated, or Failed)
- `StartedAt` - Gets the UTC timestamp when the saga was created
- `LastUpdatedAt` - Gets the UTC timestamp of the last state change (null if never updated)
- `CorrelationId` - Gets or sets the correlation ID for tracing across services
- `OutboxEvents` - Gets the read-only collection of events raised during saga processing
- `ClearOutboxEvents()` - Clears all events from the outbox
- `ToString()` - Returns a formatted string representation of the saga

Example usage:

```csharp
using System;
using System.Linq;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Domain.Sagas;

public class TransferSaga : SagaBase
{
    public override string SagaName => "TransferSaga";
    
    public string FromAccountId { get; private set; }
    public string ToAccountId { get; private set; }
    public decimal Amount { get; private set; }
    
    public TransferSaga(string fromAccountId, string toAccountId, decimal amount)
    {
        FromAccountId = fromAccountId ?? throw new ArgumentNullException(nameof(fromAccountId));
        ToAccountId = toAccountId ?? throw new ArgumentNullException(nameof(toAccountId));
        Amount = amount;
        
        // Set correlation ID for tracing
        CorrelationId = $"transfer-{FromAccountId}-{ToAccountId}";
    }
    
    // Handle domain events to progress saga state
    public void Handle(MoneyDeposited deposited)
    {
        if (State == SagaState.NotStarted && deposited.AggregateId == ToAccountId)
        {
            Activate(); // Transition to Active state
            
            // Raise an event to withdraw from source account
            RaiseEvent(new MoneyWithdrawn(
                aggregateId: FromAccountId,
                accountNumber: "FROM-ACC",
                amount: Amount,
                currency: "USD",
                reference: $"Transfer to {ToAccountId}"
            ));
        }
    }
    
    // Handle withdrawal confirmation
    public void Handle(MoneyWithdrawn withdrawn)
    {
        if (State == SagaState.Active && withdrawn.AggregateId == FromAccountId)
        {
            Complete(); // Saga completed successfully
        }
    }
    
    // Handle failure and trigger compensation
    public void Handle(TransferFailed failed)
    {
        if (State == SagaState.Active)
        {
            Compensate(); // Transition to Compensated state
            
            // Raise compensating events
            RaiseEvent(new TransferCompensated(
                aggregateId: ToAccountId,
                reference: $"Compensating transfer for failed transaction"
            ));
        }
    }
}

public class Program
{
    public static void Main()
    {
        // Create a new saga instance
        var saga = new TransferSaga("account-123", "account-456", 100.50m);
        
        Console.WriteLine($"Saga created: {saga}");
        Console.WriteLine($"Saga ID: {saga.SagaId}");
        Console.WriteLine($"Saga Name: {saga.SagaName}");
        Console.WriteLine($"State: {saga.State}");
        Console.WriteLine($"Started at: {saga.StartedAt:u}");
        Console.WriteLine($"Correlation ID: {saga.CorrelationId}");
        
        // Simulate handling events
        var depositEvent = new MoneyDeposited(
            aggregateId: "account-456",
            accountNumber: "ACC-456",
            amount: 100.50m,
            currency: "USD",
            reference: "Initial deposit"
        );
        
        saga.Handle(depositEvent);
        Console.WriteLine($"\nAfter deposit event: {saga.State}");
        Console.WriteLine($"Last updated: {saga.LastUpdatedAt?.ToString("u") ?? "null"}");
        
        // Access outbox events
        Console.WriteLine($"\nOutbox events count: {saga.OutboxEvents.Count}");
        foreach (var @event in saga.OutboxEvents)
        {
            Console.WriteLine($" - {@event.GetType().Name}");
        }
        
        // Clear outbox events after persistence
        saga.ClearOutboxEvents();
        Console.WriteLine($"\nOutbox events after clear: {saga.OutboxEvents.Count}");
        
        // Create saga with existing ID (for replay scenarios)
        var sagaWithId = new TransferSaga("account-123", "account-456", 100.50m)
        {
            SagaId = "predefined-saga-id"
        };
        Console.WriteLine($"\nSaga with custom ID: {sagaWithId.SagaId}");
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

## Transaction

`Transaction` is an immutable value object that represents a financial transaction record in the system. It captures essential details such as transaction type (deposit or withdrawal), monetary amount with currency, reference information, timestamps, and extensible metadata for additional context. Transactions are typically created during account operations and can be used for audit trails, reporting, and reconciliation.

**Public members:**
- `Id` - Unique identifier for the transaction
- `Type` - Transaction type (deposit or withdrawal) as `TransactionType` enum
- `Amount` - Monetary amount with currency as `Money` value object
- `TransactionDate` - UTC timestamp when the transaction occurred
- `Reference` - Human-readable reference text for the transaction
- `Description` - Optional description of the transaction
- `Metadata` - Dictionary for storing additional custom metadata
- `Timestamp` - Convenience property alias for `TransactionDate` (excluded from serialization)
- `Equals(Transaction? other)` - Equality comparison method
- `Equals(object? obj)` - Override for equality comparison
- `GetHashCode()` - Override for hash code generation
- `ToString()` - Returns formatted string representation

**Typical usage**

```csharp
using System;
using DotNetCqrsEventSourcing.Domain.ValueObjects;
using DotNetCqrsEventSourcing.Shared.Enums;

public class TransactionExample
{
  public void CreateAndUseTransactions()
  {
    // Create a deposit transaction
    var depositAmount = new Money(1000.50m, "USD");
    var depositTransaction = new Transaction(
      type: TransactionType.Deposit,
      amount: depositAmount,
      reference: "Salary payment",
      description: "Monthly salary deposit"
    );

    Console.WriteLine($"Deposit transaction: {depositTransaction}");
    Console.WriteLine($"Type: {depositTransaction.Type}");
    Console.WriteLine($"Amount: {depositTransaction.Amount}");
    Console.WriteLine($"Reference: {depositTransaction.Reference}");
    Console.WriteLine($"Description: {depositTransaction.Description}");
    Console.WriteLine($"Timestamp: {depositTransaction.TransactionDate:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Timestamp alias: {depositTransaction.Timestamp:yyyy-MM-dd HH:mm:ss}");

    // Create a withdrawal transaction
    var withdrawalAmount = new Money(250.75m, "USD");
    var withdrawalTransaction = new Transaction(
      type: TransactionType.Withdrawal,
      amount: withdrawalAmount,
      reference: "ATM withdrawal",
      description: "Cash withdrawal from ATM"
    );

    Console.WriteLine($"\nWithdrawal transaction: {withdrawalTransaction}");

    // Add custom metadata
    withdrawalTransaction.Metadata["Location"] = "New York City";
    withdrawalTransaction.Metadata["TerminalId"] = "ATM-001";
    withdrawalTransaction.Metadata["UserId"] = "user-123";

    Console.WriteLine($"Metadata count: {withdrawalTransaction.Metadata.Count}");
    Console.WriteLine($"Location: {withdrawalTransaction.Metadata["Location"]}");

    // Create transaction with custom ID (for replay scenarios)
    var customTransaction = new Transaction(
      id: "txn-12345",
      type: TransactionType.Deposit,
      amount: new Money(5000.00m, "EUR"),
      transactionDate: DateTime.UtcNow.AddDays(-1),
      reference: "Bonus payment",
      description: "Quarterly performance bonus"
    );

    Console.WriteLine($"\nCustom ID transaction: {customTransaction.Id}");

    // Equality comparison
    var sameTransaction = new Transaction(
      type: TransactionType.Deposit,
      amount: new Money(1000.50m, "USD"),
      reference: "Salary payment"
    );

    Console.WriteLine($"\nAre transactions equal: {depositTransaction.Equals(sameTransaction)}");
    Console.WriteLine($"Hash codes match: {depositTransaction.GetHashCode() == sameTransaction.GetHashCode()}");
  }
}
```

The example demonstrates all public members of `Transaction` with realistic usage patterns for creating, querying, and comparing transaction records.


## GetAccountQuery

`GetAccountQuery` is a query object used to retrieve a specific account by its unique identifier. This query is part of the CQRS pattern and is typically dispatched to a query handler that returns the account read model or aggregate state. The query includes correlation tracking for distributed tracing and timestamping for audit purposes.

**Public members:**
- `AccountId` - The unique identifier of the account to retrieve
- `CorrelationId` - Unique identifier for tracing the query across services
- `IssuedAt` - UTC timestamp when the query was created
- `GetAccountQuery()` - Default constructor
- `GetAccountQuery(string accountId)` - Constructor with account ID
- `ToString()` - Returns formatted string representation

**Typical usage**

```csharp
using System;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Application.Queries;
using DotNetCqrsEventSourcing.ReadModels;
using DotNetCqrsEventSourcing.Shared.Results;

public class GetAccountQueryExample
{
    private readonly IAccountReadModelQueryService _queryService;

    public GetAccountQueryExample(IAccountReadModelQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task RetrieveAccountAsync()
    {
        // Create a query to retrieve account by ID
        var query = new GetAccountQuery("account-123");
        
        Console.WriteLine($"Query created: {query}");
        Console.WriteLine($"Account ID: {query.AccountId}");
        Console.WriteLine($"Correlation ID: {query.CorrelationId}");
        Console.WriteLine($"Issued at: {query.IssuedAt:u}");

        // Execute the query to get the account
        var result = await _queryService.GetByIdAsync(query.AccountId);
        
        if (result.IsSuccess)
        {
            var account = result.Data;
            Console.WriteLine($"Account found: {account?.AccountNumber}");
            Console.WriteLine($"Account holder: {account?.AccountHolder}");
            Console.WriteLine($"Current balance: {account?.CurrentBalance}");
        }
        else
        {
            Console.WriteLine($"Account not found: {result.ErrorCode}");
        }
    }

    public void CreateQueryWithCustomCorrelation()
    {
        // Create query with custom correlation ID for tracing
        var query = new GetAccountQuery("account-456")
        {
            CorrelationId = "corr-trace-123",
            IssuedAt = DateTime.UtcNow.AddMinutes(-5) // Simulate delayed query
        };
        
        Console.WriteLine($"Custom query: {query}");
        Console.WriteLine($"Custom correlation: {query.CorrelationId}");
    }
}
```

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


## EventEnvelope

`EventEnvelope` is a wrapper for domain events that adds infrastructure metadata required for event store persistence, versioning, and integrity verification. It serves as the transport envelope for events between the domain layer and the event store, carrying both the serialized event data and essential metadata like aggregate identity, version tracking, and optional tenant partitioning for multi-tenant scenarios.

Each envelope contains the event payload (`EventData`), its type (`EventType`), the aggregate it belongs to (`AggregateId`, `AggregateType`, `AggregateVersion`), and system-generated fields like a unique identifier (`Id`), creation timestamp (`CreatedAt`), and optional integrity checksum (`ChecksumHash`) for tamper detection.

Example usage:

```csharp
using System;
using System.Text.Json;
using DotNetCqrsEventSourcing.Domain.Events;

public class EventEnvelopeExample
{
    public void CreateAndStoreEventEnvelope()
    {
        // Create a domain event
        var domainEvent = new AccountCreatedEvent("agg-123", "ACC-0001", "John Doe", "USD", 1000m)
        {
            CorrelationId = "corr-456",
            Timestamp = DateTime.UtcNow
        };

        // Serialize the event data
        var eventData = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions { WriteIndented = false });

        // Create an event envelope from the domain event
        var envelope = new EventEnvelope(domainEvent, eventData);
        
        Console.WriteLine($"Envelope created: {envelope.Id}");
        Console.WriteLine($"Aggregate: {envelope.AggregateType}#{envelope.AggregateId} v{envelope.AggregateVersion}");
        Console.WriteLine($"Event type: {envelope.EventType}");
        Console.WriteLine($"Created at: {envelope.CreatedAt:u}");
        Console.WriteLine($"Metadata count: {envelope.Metadata.Count}");

        // Compute and verify checksum for integrity
        envelope.ComputeChecksum();
        Console.WriteLine($"Checksum: {envelope.ChecksumHash}");
        Console.WriteLine($"Checksum valid: {envelope.VerifyChecksum()}");

        // Access metadata
        if (envelope.Metadata.TryGetValue("CorrelationId", out var correlationId))
        {
            Console.WriteLine($"Correlation ID: {correlationId}");
        }

        // Use ToString() for debugging
        Console.WriteLine(envelope.ToString());
    }

    public void UsePartitionKeyForMultiTenancy()
    {
        // Create an event with tenant context
        var domainEvent = new AccountCreatedEvent("agg-789", "ACC-TENANT-001", "Jane Smith", "EUR", 500m);
        var eventData = JsonSerializer.Serialize(domainEvent);
        
        // Set partition key for tenant isolation
        var envelope = new EventEnvelope(domainEvent, eventData)
        {
            PartitionKey = "tenant-abc-123"
        };

        Console.WriteLine($"Partition key: {envelope.PartitionKey}");
        Console.WriteLine($"Tenant-specific envelope: {envelope.Id}");
    }
}
```

## AccountCreatedEvent

`AccountCreatedEvent` represents the domain event that is raised when a new bank account is created in the system. This event serves as the source of truth for account creation and carries all essential information about the new account including the account number, holder details, currency, and initial balance.

The event is typically raised by the `Account` aggregate root during command processing and contains the core account information needed to initialize account state and create corresponding read models.

Example usage:

```csharp
using System;
using DotNetCqrsEventSourcing.Domain;
using DotNetCqrsEventSourcing.Domain.Events;

public class AccountCreatedEventExample
{
    public void CreateAccountAndRaiseEvent()
    {
        // Create a new account aggregate
        var account = new Account("agg-123");
        
        // Create and raise the AccountCreatedEvent
        var accountCreatedEvent = new AccountCreatedEvent(
            aggregateId: "agg-123",
            accountNumber: "ACC-2024-001",
            accountHolder: "John Doe",
            currency: "USD",
            initialBalance: 1000.00m
        );
        
        // Populate automatic metadata
        accountCreatedEvent.PopulateMetadata();
        
        // Access event properties
        Console.WriteLine($"Event ID: {accountCreatedEvent.EventId}");
        Console.WriteLine($"Account Number: {accountCreatedEvent.AccountNumber}");
        Console.WriteLine($"Account Holder: {accountCreatedEvent.AccountHolder}");
        Console.WriteLine($"Currency: {accountCreatedEvent.Currency}");
        Console.WriteLine($"Initial Balance: {accountCreatedEvent.InitialBalance:C}");
        Console.WriteLine($"Event Type: {accountCreatedEvent.GetEventType()}");
        Console.WriteLine($"Aggregate: {accountCreatedEvent.AggregateId} v{accountCreatedEvent.AggregateVersion}");
    }
    
    public void CreateAccountWithCustomMetadata()
    {
        // Create event with additional metadata
        var accountCreatedEvent = new AccountCreatedEvent(
            aggregateId: "agg-456",
            accountNumber: "ACC-2024-002",
            accountHolder: "Jane Smith",
            currency: "EUR",
            initialBalance: 5000.00m,
            occurredAt: DateTime.UtcNow.AddDays(-1)
        );
        
        // Add custom metadata
        accountCreatedEvent.UserId = "user-789";
        accountCreatedEvent.CorrelationId = "corr-xyz-123";
        accountCreatedEvent.TenantId = "tenant-abc";
        accountCreatedEvent.Metadata["IpAddress"] = "192.168.1.100";
        accountCreatedEvent.Metadata["SourceApplication"] = "WebPortal";
        
        // Populate standard metadata
        accountCreatedEvent.PopulateMetadata();
        
        Console.WriteLine($"Account created with metadata: {accountCreatedEvent.AccountNumber}");
        Console.WriteLine($"Created by user: {accountCreatedEvent.UserId}");
        Console.WriteLine($"Correlation ID: {accountCreatedEvent.CorrelationId}");
    }
    
    public void UseAccountHolderNameProperty()
    {
        // The AccountHolderName property provides convenient access to AccountHolder
        var accountCreatedEvent = new AccountCreatedEvent(
            aggregateId: "agg-789",
            accountNumber: "ACC-2024-003",
            accountHolder: "Robert Johnson",
            currency: "GBP",
            initialBalance: 750.50m
        );
        
        // Both properties reference the same value
        Console.WriteLine($"Account Holder: {accountCreatedEvent.AccountHolder}");
        Console.WriteLine($"Account Holder Name: {accountCreatedEvent.AccountHolderName}");
    }
}
```

## DomainEvent

`DomainEvent` is the abstract base class for all domain events in the CQRS + Event Sourcing framework. Domain events represent state changes in aggregates and serve as the single source of truth for the system's evolution. Each event carries metadata about the aggregate it belongs to, the user who triggered the change, correlation IDs for tracing across services, and extensible metadata storage for additional context.

Events are immutable once created and typically raised by aggregate roots during command processing. The framework provides automatic metadata population including timestamps, aggregate version tracking, and correlation management through the `PopulateMetadata` method.

Example usage:

```csharp
using System;
using System.Collections.Generic;
using DotNetCqrsEventSourcing.Domain;
using DotNetCqrsEventSourcing.Domain.Events;

public class AccountCreatedEvent : DomainEvent
{
    public string AccountNumber { get; }
    public string AccountHolder { get; }
    public string Currency { get; }
    public decimal InitialBalance { get; }

    public AccountCreatedEvent(
        string aggregateId,
        string accountNumber,
        string accountHolder,
        string currency,
        decimal initialBalance)
        : base(aggregateId)
    {
        AccountNumber = accountNumber;
        AccountHolder = accountHolder;
        Currency = currency;
        InitialBalance = initialBalance;
    }

    public override string GetEventType() => nameof(AccountCreatedEvent);
}

public class DomainEventExample
{
    public void CreateAndProcessDomainEvent()
    {
        // Create a domain event with required aggregate context
        var accountCreatedEvent = new AccountCreatedEvent(
            aggregateId: "agg-123",
            accountNumber: "ACC-0001",
            accountHolder: "John Doe",
            currency: "USD",
            initialBalance: 1000.00m)
        {
            // Optional metadata properties
            UserId = "user-456",
            CorrelationId = "corr-789",
            TenantId = "tenant-abc"
        };

        // Populate automatic metadata
        accountCreatedEvent.PopulateMetadata();

        // Access event properties
        Console.WriteLine($"Event ID: {accountCreatedEvent.EventId}");
        Console.WriteLine($"Aggregate: {accountCreatedEvent.AggregateId} ({accountCreatedEvent.AggregateType}) v{accountCreatedEvent.AggregateVersion}");
        Console.WriteLine($"Occurred at: {accountCreatedEvent.OccurredAt:u}");
        Console.WriteLine($"User: {accountCreatedEvent.UserId}");
        Console.WriteLine($"Correlation: {accountCreatedEvent.CorrelationId}");
        Console.WriteLine($"Tenant: {accountCreatedEvent.TenantId}");
        Console.WriteLine($"Event type: {accountCreatedEvent.GetEventType()}");
        Console.WriteLine($"Metadata count: {accountCreatedEvent.Metadata.Count}");

        // Access custom metadata
        if (accountCreatedEvent.Metadata.TryGetValue("CustomField", out var customValue))
        {
            Console.WriteLine($"Custom metadata: {customValue}");
        }

        // String representation for debugging
        Console.WriteLine(accountCreatedEvent.ToString());
    }

    public void CreateEventWithCustomMetadata()
    {
        // Create an event with custom metadata
        var eventWithMetadata = new AccountCreatedEvent(
            aggregateId: "agg-456",
            accountNumber: "ACC-0002",
            accountHolder: "Jane Smith",
            currency: "EUR",
            initialBalance: 500.00m)
        {
            UserId = "user-789",
            CorrelationId = "corr-xyz",
            TenantId = "tenant-def"
        };

        // Add custom metadata
        eventWithMetadata.Metadata["IpAddress"] = "192.168.1.100";
        eventWithMetadata.Metadata["UserAgent"] = "Mozilla/5.0";
        eventWithMetadata.Metadata["SourceApplication"] = "WebApp";

        // Populate standard metadata
        eventWithMetadata.PopulateMetadata();

        Console.WriteLine($"Event with custom metadata: {eventWithMetadata.EventId}");
        Console.WriteLine($"IP Address: {eventWithMetadata.Metadata["IpAddress"]}");
    }
}
```

## Account

`Account` is the aggregate root that manages the complete lifecycle of a bank account within the CQRS + Event Sourcing framework. It handles account creation, deposits, withdrawals, and closure while maintaining a transaction history and balance state. All state changes are persisted as a sequence of immutable domain events.

The account tracks its version, timestamps, and supports event replay for state reconstruction. It also provides snapshot support through `LastSnapshotVersion` for performance optimization in systems with frequent aggregate access.

**Public members:**
- `AccountNumber` - The unique account identifier
- `AccountHolder` - The name of the account holder
- `Balance` - The current balance as a `Balance` value object
- `Status` - The account status (`Active` or `Closed`)
- `Transactions` - List of transaction history entries
- `OpenDate` - When the account was opened
- `CloseDate` - When the account was closed (null if still open)
- `LastSnapshotVersion` - Version at which the last snapshot was taken
- `AccountHolderName` - Convenience alias for `AccountHolder`
- `IsClosed` - Boolean indicating if account is closed
- `ReplayEvents(IEnumerable<DomainEvent>)` - Replays events to reconstruct state
- `CreateAccount(string, string, string, decimal)` - Creates and opens a new account
- `Deposit(decimal, string)` - Deposits funds into the account
- `Withdraw(decimal, string)` - Withdraws funds from the account
- `CloseAccount(string)` - Closes the account
- `ToString()` - Returns formatted account information

Example usage:

```csharp
using System;
using System.Linq;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;
using DotNetCqrsEventSourcing.Domain.Events;

public class AccountExample
{
    public void CreateAndManageAccount()
    {
        // Create a new account aggregate instance
        var account = new Account("ACC-2024-001");

        // Create and open the account with initial balance
        account.CreateAccount(
            accountNumber: "ACC-2024-001",
            accountHolder: "John Doe",
            currency: "USD",
            initialBalance: 1000.00m
        );

        Console.WriteLine($"Account created: {account}");
        Console.WriteLine($"Initial balance: {account.Balance.CurrentAmount}");
        Console.WriteLine($"Account holder: {account.AccountHolder}");
        Console.WriteLine($"Status: {account.Status}");
        Console.WriteLine($"Open date: {account.OpenDate:yyyy-MM-dd}");
        Console.WriteLine($"Version: {account.Version}");

        // Deposit funds
        account.Deposit(500.00m, "Salary payment");
        Console.WriteLine($"\nAfter deposit: Balance = {account.Balance.CurrentAmount}");
        Console.WriteLine($"Transactions count: {account.Transactions.Count}");

        // Withdraw funds
        account.Withdraw(200.00m, "Rent payment");
        Console.WriteLine($"\nAfter withdrawal: Balance = {account.Balance.CurrentAmount}");
        Console.WriteLine($"Transactions count: {account.Transactions.Count}");

        // Access transaction history
        var lastTransaction = account.Transactions.Last();
        Console.WriteLine($"\nLast transaction: {lastTransaction.Type} of {lastTransaction.Amount} {lastTransaction.Amount.Currency}");
        Console.WriteLine($"Reference: {lastTransaction.Reference}");

        // Close the account
        account.CloseAccount("Account closed by customer");
        Console.WriteLine($"\nAccount closed: Status = {account.Status}");
        Console.WriteLine($"Close date: {account.CloseDate?.ToString("yyyy-MM-dd") ?? "null"}");

        // Get all uncommitted events for persistence
        var uncommittedEvents = account.GetUncommittedEvents();
        Console.WriteLine($"\nUncommitted events to persist: {uncommittedEvents.Count}");
        Console.WriteLine("Event types:");
        foreach (var @event in uncommittedEvents)
        {
            Console.WriteLine($"  - {@event.GetType().Name}");
        }

        // Clear events after persistence
        account.ClearUncommittedEvents();
        Console.WriteLine($"\nEvents after clearing: {account.GetUncommittedEvents().Count}");
    }

    public void ReplayAccountHistory()
    {
        // Create account from existing events (simulating event replay)
        var account = new Account("ACC-2024-002");

        // Simulate loading from event history
        var events = new DomainEvent[]
        {
            new AccountCreatedEvent(
                aggregateId: "ACC-2024-002",
                accountNumber: "ACC-2024-002",
                accountHolder: "Jane Smith",
                currency: "EUR",
                initialBalance: 5000.00m
            ),
            new MoneyDepositedEvent(
                aggregateId: "ACC-2024-002",
                amount: 1500.00m,
                reference: "Bonus payment",
                aggregateVersion: 2
            ),
            new MoneyWithdrawnEvent(
                aggregateId: "ACC-2024-002",
                amount: 800.00m,
                reference: "Groceries",
                aggregateVersion: 3
            )
        };

        // Replay events to reconstruct state
        account.ReplayEvents(events);

        Console.WriteLine("Replayed account state:");
        Console.WriteLine(account.ToString());
        Console.WriteLine($"Current balance: {account.Balance.CurrentAmount}");
        Console.WriteLine($"Transaction count: {account.Transactions.Count}");
    }

    public void CheckAccountStatus()
    {
        var account = new Account("ACC-2024-003");
        account.CreateAccount("ACC-2024-003", "Bob Johnson", "GBP", 2500.00m);

        // Use convenience properties
        Console.WriteLine($"Account holder name: {account.AccountHolderName}");
        Console.WriteLine($"Is account closed: {account.IsClosed}");
        Console.WriteLine($"Account status: {account.Status}");

        // Perform operations
        account.Deposit(1000.00m, "Freelance work");
        account.Withdraw(300.00m, "Utilities");

        // Display final state
        Console.WriteLine($"\nFinal account state: {account}");
    }
}
```

## CommandExtensions

`CommandExtensions` provides a fluent API for command processing with validation, correlation tracking, and error handling. It extends the core command processing capabilities with extension methods that simplify command execution, event enrichment, and validation workflows.

This class is particularly useful for:
- Adding correlation tracking to commands and events
- Validating command properties before execution
- Enriching domain events with contextual metadata
- Executing commands with proper error handling and result propagation

The extension methods follow a fluent pattern that integrates well with the CQRS pattern and Result{T} monad used throughout the framework.

Example usage:

```csharp
using System;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Application.Extensions;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Shared.Results;

public class CommandExtensionsExample
{
    public async Task ExecuteValidatedCommandAsync()
    {
        // Create a command handler that returns a Result
        Func<CancellationToken, Task<Result<string>>> commandHandler = async (ct) =>
        {
            // Simulate command processing
            await Task.Delay(100, ct);
            return Result<string>.Success("Command executed successfully");
        };

        // Execute the command with built-in error handling
        var result = await commandHandler.ExecuteCommandAsync();

        if (result.IsSuccess)
        {
            Console.WriteLine($"Success: {result.Value}");
        }
        else
        {
            Console.WriteLine("Errors:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($" - {error}");
            }
        }
    }

    public void AddCorrelationTracking()
    {
        // Create a command object
        var createAccountCommand = new CreateAccountCommand
        {
            AccountNumber = "ACC-12345",
            AccountHolder = "John Doe",
            InitialBalance = 1000.00m,
            Currency = "USD"
        };

        // Get or create correlation ID for tracing
        var correlationId = createAccountCommand.GetOrCreateCorrelationId();
        Console.WriteLine($"Correlation ID: {correlationId}");

        // Use correlation ID when creating events
        var accountCreatedEvent = new AccountCreatedEvent(
            aggregateId: "agg-123",
            accountNumber: "ACC-12345",
            accountHolder: "John Doe",
            currency: "USD",
            initialBalance: 1000.00m
        );

        // Enrich event with correlation tracking
        accountCreatedEvent = accountCreatedEvent.EnrichEvent(correlationId: correlationId);
        Console.WriteLine($"Event correlation: {accountCreatedEvent.CorrelationId}");
    }

    public void ValidateCommandProperties()
    {
        // Create a command with validation requirements
        var command = new CreateAccountCommand
        {
            AccountNumber = "", // Invalid - empty
            AccountHolder = "John Doe",
            InitialBalance = -100.00m, // Invalid - negative
            Currency = "USD"
        };

        // Validate command properties
        var validationErrors = command.Validate();
        
        if (validationErrors.Any())
        {
            Console.WriteLine("Validation errors:");
            foreach (var error in validationErrors)
            {
                Console.WriteLine($" - {error}");
            }
        }
    }

    public void CreateEventFromCommand()
    {
        // Create a command
        var depositCommand = new DepositCommand
        {
            AccountId = "ACC-12345",
            Amount = 500.00m,
            Reference = "Salary payment"
        };

        // Create event from command using factory pattern
        var moneyDepositedEvent = depositCommand.CreateEventFromCommand(cmd => new MoneyDepositedEvent(
            aggregateId: cmd.AccountId,
            amount: cmd.Amount,
            reference: cmd.Reference,
            currency: "USD"
        ));

        Console.WriteLine($"Created event from command: {moneyDepositedEvent.GetType().Name}");
        Console.WriteLine($"Event correlation: {moneyDepositedEvent.CorrelationId}");
    }
}

// Example command classes
public class CreateAccountCommand
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class DepositCommand
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
}
```

## AggregateRoot

`AggregateRoot` is the base class for all aggregate roots in the CQRS + Event Sourcing framework. It provides the foundation for implementing domain-driven aggregates by managing event sourcing, state reconstruction, and version tracking. Aggregate roots are the only objects in the domain layer that can raise domain events, ensuring that all state changes are captured as a sequence of immutable events.

Each aggregate root maintains a list of uncommitted events that are raised during command processing and can be persisted to the event store. The framework automatically tracks aggregate versioning, timestamps, and optional tenant isolation for multi-tenant scenarios.

Example usage:

```csharp
using System;
using DotNetCqrsEventSourcing.Domain;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;
using DotNetCqrsEventSourcing.Domain.Events;

public class Account : AggregateRoot
{
    public string AccountNumber { get; private set; }
    public string AccountHolder { get; private set; }
    public decimal Balance { get; private set; }
    public bool IsClosed { get; private set; }

    // Create new account aggregate
    public Account(string accountNumber, string accountHolder, decimal initialBalance)
    {
        if (string.IsNullOrEmpty(accountNumber))
            throw new ArgumentException("Account number is required", nameof(accountNumber));
        if (string.IsNullOrEmpty(accountHolder))
            throw new ArgumentException("Account holder is required", nameof(accountHolder));
        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));

        AccountNumber = accountNumber;
        AccountHolder = accountHolder;
        Balance = initialBalance;

        // Raise domain event for account creation
        RaiseEvent(new AccountCreatedEvent(
            aggregateId: Id,
            accountNumber: accountNumber,
            accountHolder: accountHolder,
            currency: "USD",
            initialBalance: initialBalance
        ));
    }

    // Protected constructor for replay
    protected Account() { }

    // Apply domain event to state
    protected override void ApplyEvent(DomainEvent @event, bool isFromHistory)
    {
        switch (@event)
        {
            case AccountCreatedEvent created:
                AccountNumber = created.AccountNumber;
                AccountHolder = created.AccountHolder;
                Balance = created.InitialBalance;
                IsClosed = false;
                break;

            case MoneyDeposited deposited:
                Balance += deposited.Amount;
                break;

            case MoneyWithdrawn withdrawn:
                if (Balance < withdrawn.Amount)
                    throw new InvalidOperationException("Insufficient funds");
                Balance -= withdrawn.Amount;
                break;

            case AccountClosed _:
                IsClosed = true;
                break;
        }
    }

    // Business operations
    public void Deposit(decimal amount, string reference)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot deposit to a closed account");
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive", nameof(amount));

        RaiseEvent(new MoneyDeposited(
            aggregateId: Id,
            accountNumber: AccountNumber,
            amount: amount,
            currency: "USD",
            reference: reference
        ));
    }

    public void Withdraw(decimal amount, string reference)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot withdraw from a closed account");
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive", nameof(amount));
        if (Balance < amount)
            throw new InvalidOperationException("Insufficient funds");

        RaiseEvent(new MoneyWithdrawn(
            aggregateId: Id,
            accountNumber: AccountNumber,
            amount: amount,
            currency: "USD",
            reference: reference
        ));
    }

    public void Close()
    {
        if (IsClosed)
            throw new InvalidOperationException("Account is already closed");

        RaiseEvent(new AccountClosed(Id));
    }
}

public class Program
{
    public static void Main()
    {
        // Create new account aggregate
        var account = new Account("ACC-001", "John Doe", 1000.00m);
        
        Console.WriteLine(account.ToString());
        Console.WriteLine($"Initial balance: {account.Balance}");
        Console.WriteLine($"Uncommitted events: {account.GetUncommittedEvents().Count}");

        // Apply business operations
        account.Deposit(500.00m, "Salary");
        account.Withdraw(200.00m, "Rent");

        Console.WriteLine($"Balance after transactions: {account.Balance}");
        Console.WriteLine($"Version: {account.Version}");
        Console.WriteLine($"Created at: {account.CreatedAt:u}");
        Console.WriteLine($"Updated at: {account.UpdatedAt:u}");

        // Get uncommitted events for persistence
        var uncommittedEvents = account.GetUncommittedEvents();
        Console.WriteLine($"Events to persist: {uncommittedEvents.Count}");
        
        // Clear events after persistence
        account.ClearUncommittedEvents();
        Console.WriteLine($"Events after clear: {account.GetUncommittedEvents().Count}");

        // Example with tenant isolation
        var tenantAccount = new Account("ACC-TENANT-001", "Tenant User", 500.00m)
        {
            TenantId = "tenant-abc-123"
        };
        
        Console.WriteLine($"Tenant account: {tenantAccount.TenantId}");
    }
}
```

## Balance

`Balance` is a value object representing an account balance with transaction tracking. It maintains the current balance, available funds, held funds, and transaction count while providing methods for adding funds, removing funds, and managing holds. The balance automatically tracks the last update timestamp and supports equality comparisons for state validation.

**Public members:**
- `CurrentAmount` - The total current balance including all transactions
- `AvailableAmount` - The amount available for withdrawal or transfer
- `HoldAmount` - The amount currently on hold (reserved but not withdrawn)
- `LastUpdated` - When the balance was last modified
- `TransactionCount` - The total number of balance-changing operations
- `AddFunds(Money amount)` - Adds funds to the balance
- `RemoveFunds(Money amount)` - Removes funds from the balance (with validation)
- `PlaceHold(Money amount)` - Places a hold on available funds
- `ReleaseHold(Money amount)` - Releases a previously placed hold
- `Equals(Balance? other)` - Equality comparison
- `Equals(object? obj)` - Override for equality comparison
- `GetHashCode()` - Override for hash code generation
- `ToString()` - Returns formatted balance information

Example usage:

```csharp
using System;
using DotNetCqrsEventSourcing.Domain.ValueObjects;

public class BalanceExample
{
    public void ManageAccountBalance()
    {
        // Create initial balance with 1000 USD
        var initialBalance = new Money(1000.00m, "USD");
        var balance = new Balance(initialBalance);
        
        Console.WriteLine($"Initial balance: {balance}");
        Console.WriteLine($"Current: {balance.CurrentAmount}");
        Console.WriteLine($"Available: {balance.AvailableAmount}");
        Console.WriteLine($"Hold: {balance.HoldAmount}");
        Console.WriteLine($"Last updated: {balance.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Transaction count: {balance.TransactionCount}");
        
        // Add funds (deposit)
        var depositAmount = new Money(500.00m, "USD");
        balance.AddFunds(depositAmount);
        
        Console.WriteLine($"\nAfter deposit of {depositAmount}:");
        Console.WriteLine($"Current: {balance.CurrentAmount}");
        Console.WriteLine($"Available: {balance.AvailableAmount}");
        Console.WriteLine($"Transaction count: {balance.TransactionCount}");
        
        // Place a hold on funds (e.g., for a pending transaction)
        var holdAmount = new Money(200.00m, "USD");
        balance.PlaceHold(holdAmount);
        
        Console.WriteLine($"\nAfter placing hold of {holdAmount}:");
        Console.WriteLine($"Current: {balance.CurrentAmount}");
        Console.WriteLine($"Available: {balance.AvailableAmount}");
        Console.WriteLine($"Hold: {balance.HoldAmount}");
        
        // Release part of the hold
        var releaseAmount = new Money(100.00m, "USD");
        balance.ReleaseHold(releaseAmount);
        
        Console.WriteLine($"\nAfter releasing {releaseAmount} of hold:");
        Console.WriteLine($"Current: {balance.CurrentAmount}");
        Console.WriteLine($"Available: {balance.AvailableAmount}");
        Console.WriteLine($"Hold: {balance.HoldAmount}");
        
        // Remove funds (withdrawal)
        var withdrawalAmount = new Money(300.00m, "USD");
        balance.RemoveFunds(withdrawalAmount);
        
        Console.WriteLine($"\nAfter withdrawal of {withdrawalAmount}:");
        Console.WriteLine($"Current: {balance.CurrentAmount}");
        Console.WriteLine($"Available: {balance.AvailableAmount}");
        Console.WriteLine($"Transaction count: {balance.TransactionCount}");
        
        // Check balance state
        Console.WriteLine($"\nFinal balance state: {balance}");
        
        // Equality comparison
        var sameBalance = new Balance(new Money(1200.00m, "USD"));
        Console.WriteLine($"\nAre balances equal: {balance.Equals(sameBalance)}");
    }
}
```

## SagaOrchestrator

`SagaOrchestrator` is the central coordinator that routes domain events to registered saga handlers, processes saga state transitions, and publishes outbox events produced by sagas. It acts as the bridge between the event bus and saga handlers, ensuring that all saga-related operations are properly managed and tracked.

The orchestrator maintains a collection of saga handler wrappers, dispatches events to capable handlers based on event type matching, and handles the publication of any events that sagas raise during processing. It also provides comprehensive logging and error handling to ensure saga execution is observable and resilient.

**Key responsibilities:**
- Event routing to saga handlers based on event type compatibility
- Managing saga lifecycle and state transitions
- Collecting and publishing outbox events from sagas
- Error handling and logging for saga processing failures
- Supporting saga handler registration via dependency injection




**Public members:**
- `DispatchAsync(DomainEvent, CancellationToken)` - Routes an event to all capable saga handlers and publishes their outbox events

Example usage:

```csharp
using System;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Application.Sagas;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Domain.Sagas;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Define a custom saga that handles account creation events
public class AccountWelcomeSaga : SagaBase
{
    public override string SagaName => "AccountWelcomeSaga";
    
    public string AccountId { get; private set; }
    public string UserEmail { get; private set; }
    
    public AccountWelcomeSaga() { }
    
    public AccountWelcomeSaga(string accountId, string userEmail)
    {
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        UserEmail = userEmail ?? throw new ArgumentNullException(nameof(userEmail));
        CorrelationId = $"welcome-{accountId}";
    }
    
    // Handle AccountCreatedEvent to send welcome email
    public void Handle(AccountCreatedEvent @event)
    {
        if (State == SagaState.NotStarted)
        {
            Activate();
            
            // Raise an event to trigger welcome email service
            RaiseEvent(new WelcomeEmailSent(
                aggregateId: @event.AggregateId,
                email: UserEmail,
                accountId: @event.AccountNumber,
                initialBalance: @event.InitialBalance
            ));
            
            Complete(); // Saga completed successfully
        }
    }
}

// Define a saga handler interface
public interface IAccountWelcomeSagaHandler : ISagaHandler<AccountWelcomeSaga, AccountCreatedEvent> { }

// Implement the saga handler
public class AccountWelcomeSagaHandler : IAccountWelcomeSagaHandler
{
    private readonly ILogger<AccountWelcomeSagaHandler> _logger;
    
    public AccountWelcomeSagaHandler(ILogger<AccountWelcomeSagaHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task<Result> HandleAsync(AccountCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing account welcome saga for account {AccountNumber}", @event.AccountNumber);
        
        // In a real application, this would send an actual email
        // For now, just log and return success
        await Task.Delay(100, cancellationToken);
        
        Console.WriteLine($"Welcome saga completed for account: {@event.AccountNumber}");
        return Result.Success();
    }
}

// Setup and usage in application
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup DI container
        var services = new ServiceCollection();
        
        services.AddLogging(configure => configure.AddConsole());
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton<IAccountWelcomeSagaHandler, AccountWelcomeSagaHandler>();
        
        // Register saga handler wrapper
        services.AddSingleton<ISagaHandlerWrapper>(provider => 
            new SagaHandlerWrapper<AccountWelcomeSaga, AccountCreatedEvent>(
                provider.GetRequiredService<IAccountWelcomeSagaHandler>()
            )
        );
        
        // Build service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Get orchestrator and event bus
        var orchestrator = new SagaOrchestrator(
            handlers: new[] { serviceProvider.GetRequiredService<ISagaHandlerWrapper>() },
            eventBus: serviceProvider.GetRequiredService<IEventBus>(),
            logger: serviceProvider.GetRequiredService<ILogger<SagaOrchestrator>>()
        );
        
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        
        // Create and dispatch an account created event
        var accountCreatedEvent = new AccountCreatedEvent(
            aggregateId: "agg-123",
            accountNumber: "ACC-2024-001",
            accountHolder: "John Doe",
            currency: "USD",
            initialBalance: 1000.00m
        );
        accountCreatedEvent.PopulateMetadata();
        
        Console.WriteLine("Dispatching account created event to saga orchestrator...");
        
        // Dispatch the event
        var result = await orchestrator.DispatchAsync(accountCreatedEvent);
        
        if (result.IsSuccess)
        {
            Console.WriteLine("Saga processing completed successfully!");
        }
        else
        {
            Console.WriteLine($"Saga processing failed: {result.ErrorMessage}");
        }
        
        // Check for any outbox events that were produced
        var wrapper = serviceProvider.GetRequiredService<ISagaHandlerWrapper>() as SagaHandlerWrapper<AccountWelcomeSaga, AccountCreatedEvent>;
        var outboxEvents = wrapper?.DrainOutboxEvents();
        
        if (outboxEvents != null && outboxEvents.Count > 0)
        {
            Console.WriteLine($"Saga produced {outboxEvents.Count} outbox event(s):");
            foreach (var @event in outboxEvents)
            {
                Console.WriteLine($" - {@event.GetType().Name}");
            }
        }
    }
}

// Supporting domain event for welcome saga
public class WelcomeEmailSent : DomainEvent
{
    public string Email { get; }
    public string AccountId { get; }
    public decimal InitialBalance { get; }
    
    public WelcomeEmailSent(string aggregateId, string email, string accountId, decimal initialBalance)
        : base(aggregateId)
    {
        Email = email;
        AccountId = accountId;
        InitialBalance = initialBalance;
    }
    
    public override string GetEventType() => nameof(WelcomeEmailSent);
}
```

## EventHandlers

`EventHandlers` is the central event handling coordinator in the CQRS + Event Sourcing framework. It manages the registration of domain event handlers and coordinates event processing across the system. The class provides a centralized location for subscribing to domain events and delegating their processing to appropriate handlers.

The `EventHandlers` class coordinates:
- Domain event subscriptions and routing
- Projection updates for read models  
- Snapshot creation for aggregates
- Error handling and logging for event processing

Example usage:

```csharp
using System;
using DotNetCqrsEventSourcing.Application.Handlers;
using DotNetCqrsEventSourcing.Domain.Events;
using Microsoft.Extensions.Logging;

public class EventHandlersExample
{
    private readonly EventHandlers _eventHandlers;
    private readonly ILoggerFactory _loggerFactory;

    public EventHandlersExample()
    {
        // Setup dependencies (in real app these would be injected)
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        // EventHandlers would typically be registered as a singleton service
        _eventHandlers = new EventHandlers(
            eventBus: new InMemoryEventBus(),
            projectionService: new InMemoryProjectionService(),
            snapshotService: new InMemorySnapshotService(),
            logger: _loggerFactory.CreateLogger<EventHandlers>()
        );
    }

    public void SetupEventHandlers()
    {
        // Register all event handlers
        _eventHandlers.RegisterHandlers();
        
        Console.WriteLine("Event handlers registered successfully");
    }

    public void CustomEventHandlerExample()
    {
        // Example of custom event handler implementation
        var customHandler = new CustomAccountEventHandler(
            _loggerFactory.CreateLogger<CustomAccountEventHandler>()
        );

        // Handle a domain event
        var accountCreatedEvent = new AccountCreatedEvent(
            aggregateId: "agg-123",
            accountNumber: "ACC-0001",
            accountHolder: "John Doe",
            currency: "USD",
            initialBalance: 1000.00m
        );

        customHandler.HandleAsync(accountCreatedEvent).Wait();
    }
}

// Custom event handler implementation
public class CustomAccountEventHandler : EventHandler<AccountCreatedEvent>
{
    public CustomAccountEventHandler(ILogger logger) : base(logger) { }

    public override async Task HandleAsync(AccountCreatedEvent @event)
    {
        Logger.LogInformation("Custom handler processing account creation: {AccountNumber}", @event.AccountNumber);
        
        // Custom business logic for account creation
        await Task.Delay(100); // Simulate processing
        
        Console.WriteLine($"Account created: {@event.AccountNumber} for {@event.AccountHolder}");
    }

    public override async Task HandleErrorAsync(AccountCreatedEvent @event, Exception exception)
    {
        Logger.LogError(exception, "Failed to process account creation for {AccountNumber}", @event.AccountNumber);
        await Task.CompletedTask;
    }
}

// Saga example
public class TransferSaga : EventSaga
{
    private string _sagaId = Guid.NewGuid().ToString();
    private string _fromAccountId;
    private string _toAccountId;
    private decimal _amount;

    public TransferSaga(IEventBus eventBus, ILogger logger) 
        : base(eventBus, logger) { }

    public override async Task StartAsync(DomainEvent triggeringEvent)
    {
        if (triggeringEvent is AccountCreatedEvent accountEvent)
        {
            _fromAccountId = accountEvent.AggregateId;
            Logger.LogInformation("Transfer saga started for account: {AccountId}", _fromAccountId);
        }
        
        await Task.CompletedTask;
    }

    public override async Task CompleteStepAsync(DomainEvent @event)
    {
        Logger.LogInformation("Saga step completed for event: {EventType}", @event.GetType().Name);
        await Task.CompletedTask;
    }

    public override async Task CompensateAsync(string sagaId)
    {
        Logger.LogWarning("Compensating saga: {SagaId}", sagaId);
        await Task.CompletedTask;
    }
}
```

## AggregateSnapshot

`AggregateSnapshot` is a complete state snapshot of an aggregate root at a specific version. It captures the entire serialized state of an aggregate, allowing for efficient reconstruction of aggregate state without replaying the entire event history. Snapshots are particularly useful for aggregates with long event streams, as they significantly reduce the time required for state reconstruction during event replay.

Aggregate snapshots support compression for storage optimization and include checksum verification for data integrity. They are automatically created by the event store compaction service when configured thresholds are reached, and can be used as the base for incremental snapshots that record only state changes between snapshots.

Example usage:

```csharp
using System;
using System.Text.Json;
using DotNetCqrsEventSourcing.Domain.Snapshots;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;

public class AggregateSnapshotExample
{
    public void CreateAndUseAggregateSnapshot()
    {
        // Create an account aggregate with some state
        var account = new Account("ACC-123", "John Doe", 1000.00m);
        
        // Simulate some operations that would normally raise events
        account.Deposit(500.00m, "Salary");
        account.Withdraw(200.00m, "Rent");
        
        // Get the current state as a snapshot
        var snapshot = new AggregateSnapshot
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = account.Id,
            AggregateType = typeof(Account).FullName!,
            Version = account.Version,
            AggregateData = JsonSerializer.Serialize(account),
            CreatedAt = DateTime.UtcNow
        };
        
        // Compute checksum for integrity verification
        snapshot.ComputeChecksum();
        
        Console.WriteLine($"Created snapshot: {snapshot.Id}");
        Console.WriteLine($"Aggregate: {snapshot.AggregateType}#{snapshot.AggregateId} v{snapshot.Version}");
        Console.WriteLine($"Created at: {snapshot.CreatedAt:u}");
        Console.WriteLine($"Checksum valid: {snapshot.VerifyChecksum()}");
        Console.WriteLine($"Data size: {snapshot.AggregateData.Length} characters");
        
        // Access size information
        Console.WriteLine($"Compressed size: {snapshot.CompressedSize} bytes");
        Console.WriteLine($"Uncompressed size: {snapshot.UncompressedSize} bytes");
        Console.WriteLine($"Is compressed: {snapshot.IsCompressed}");
        Console.WriteLine($"Compression ratio: {snapshot.GetCompressionRatio():P2}");
        
        // Mark as compressed for storage optimization
        snapshot.MarkCompressed();
        Console.WriteLine($"After compression - Size in KB: {snapshot.GetSizeInKilobytes()}");
        
        // String representation for debugging
        Console.WriteLine($"\nSnapshot details: {snapshot}");
    } 
    
    public void UseSnapshotForStateReconstruction()
    {
        // In a real application, snapshots are created by the event store compaction service
        // Here we simulate loading a snapshot to reconstruct aggregate state
        
        var snapshotData = new
        {
            Id = "ACC-123",
            AccountHolder = "Jane Smith",
            Balance = 1500.75m,
            Status = "Active",
            OpenDate = DateTime.UtcNow.AddDays(-30),
            Transactions = new[] { "Deposit: 1000", "Deposit: 500" }
        };
        
        // Create snapshot from serialized data
        var snapshot = new AggregateSnapshot
        {
            Id = "snap-456",
            AggregateId = "agg-456",
            AggregateType = "Account",
            Version = 15,
            AggregateData = JsonSerializer.Serialize(snapshotData),
            CreatedAt = DateTime.UtcNow,
            Checksum = "valid-checksum-here"
        };
        
        // Verify snapshot integrity before using it
        if (snapshot.VerifyChecksum())
        {
            Console.WriteLine("Snapshot integrity verified - safe to use for state reconstruction");
            
            // Deserialize the aggregate state from the snapshot
            var reconstructedState = JsonSerializer.Deserialize<object>(snapshot.AggregateData);
            Console.WriteLine($"Reconstructed state from snapshot v{snapshot.Version}");
        }
        else
        {
            Console.WriteLine("Snapshot checksum verification failed - do not use this snapshot");
        }
    }
}
```

## IncrementalSnapshot

`IncrementalSnapshot` represents a lightweight delta snapshot that records only the state changes that occurred since a base `AggregateSnapshot` (or a previous incremental). This approach reduces storage costs and write latency by avoiding repeated serialization of the full aggregate state. Incremental snapshots form chains anchored to a base snapshot, allowing efficient reconstruction of aggregate state through incremental deltas.

**Public members:**
- `Id` - Unique identifier for this incremental snapshot
- `AggregateId` - Aggregate identifier this snapshot belongs to
- `AggregateType` - Fully-qualified type name of the aggregate
- `Version` - Aggregate version captured by this delta
- `BaseVersion` - Version of the preceding snapshot this delta is relative to
- `BaseSnapshotId` - ID of the immediately preceding snapshot
- `DeltaData` - JSON-serialized dictionary mapping changed field paths to their new values
- `SequenceNumber` - 1-based position within the incremental chain for this aggregate
- `IsCompressed` - Whether `DeltaData` has been GZip-compressed
- `CreatedAt` - UTC timestamp when this incremental snapshot was persisted
- `Checksum` - SHA-256 checksum for integrity verification
- `EventDelta` - Number of aggregate versions bridged by this incremental snapshot (Version - BaseVersion)

**Methods:**
- `Create()` - Factory method that creates an incremental snapshot and computes its checksum
- `ComputeChecksum()` - Computes and stores a SHA-256 checksum across key fields
- `VerifyChecksum()` - Verifies the stored checksum; returns false if absent or mismatched
- `ToString()` - Returns formatted string representation

Example usage:

```csharp
using System;
using System.Text.Json;
using DotNetCqrsEventSourcing.Domain.Snapshots;

public class IncrementalSnapshotExample
{
    public void CreateAndUseIncrementalSnapshot()
    {
        // Create a base snapshot (typically created after significant state changes)
        var baseSnapshot = new AggregateSnapshot
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = "account-123",
            AggregateType = "Account",
            Version = 10,
            SnapshotData = JsonSerializer.Serialize(new { Balance = 1000.50m, Status = "Active" }),
            CreatedAt = DateTime.UtcNow
        };

        // Create an incremental snapshot representing changes from version 10 to 15
        var incremental1 = IncrementalSnapshot.Create(
            aggregateId: "account-123",
            aggregateType: "Account",
            baseSnapshotId: baseSnapshot.Id,
            baseVersion: 10,
            currentVersion: 15,
            deltaData: JsonSerializer.Serialize(new { Balance = 1500.75m }),
            sequenceNumber: 1
        );

        Console.WriteLine($"Created incremental snapshot: {incremental1}");
        Console.WriteLine($"Aggregate: {incremental1.AggregateId} v{incremental1.Version}");
        Console.WriteLine($"Base version: {incremental1.BaseVersion}, Delta: {incremental1.EventDelta} events");
        Console.WriteLine($"Checksum valid: {incremental1.VerifyChecksum()}");

        // Create another incremental representing further changes from version 15 to 20
        var incremental2 = IncrementalSnapshot.Create(
            aggregateId: "account-123",
            aggregateType: "Account",
            baseSnapshotId: incremental1.Id,
            baseVersion: 15,
            currentVersion: 20,
            deltaData: JsonSerializer.Serialize(new { Status = "Closed", CloseDate = DateTime.UtcNow }),
            sequenceNumber: 2
        );

        Console.WriteLine($"\nCreated second incremental: {incremental2}");
        Console.WriteLine($"Event delta: {incremental2.EventDelta} events");

        // Create a snapshot chain for efficient reconstruction
        var chain = new IncrementalSnapshotChain(baseSnapshot, new[] { incremental1, incremental2 });
        Console.WriteLine($"\nSnapshot chain: {chain}");
        Console.WriteLine($"Total chain length: {chain.Length} (1 base + {chain.Incrementals.Count} incrementals)");
        Console.WriteLine($"Current version via chain: {chain.CurrentVersion}");

        // Verify chain should collapse when too many incrementals accumulate
        var longChain = new IncrementalSnapshotChain(baseSnapshot, Enumerable.Range(1, 15)
            .Select(i => IncrementalSnapshot.Create(
                aggregateId: "account-123",
                aggregateType: "Account",
                baseSnapshotId: i == 1 ? baseSnapshot.Id : $"inc-{i-1}",
                baseVersion: i == 1 ? 10 : i * 5,
                currentVersion: (i + 1) * 5,
                deltaData: JsonSerializer.Serialize(new { Balance = (1000 + (i * 100)).ToString() }),
                sequenceNumber: i
            )));

        Console.WriteLine($"\nLong chain should collapse: {longChain.ShouldCollapse()}");
        Console.WriteLine($"Should collapse with max 5: {longChain.ShouldCollapse(maxIncrementals: 5)}");
    }
}
```

## Money

Example usage:

```csharp
using System;
using DotNetCqrsEventSourcing.Domain.ValueObjects;

public class MoneyExample
{
    public void CreateAndUseMoneyValues()
    {
        // Create money values in different currencies
        var usdAmount = new Money(1000.50m, "USD");
        var eurAmount = new Money(750.25m, "EUR");
        var gbpAmount = new Money(500.00m, "GBP");

        Console.WriteLine($"{usdAmount}");
        Console.WriteLine($"{eurAmount}");
        Console.WriteLine($"{gbpAmount}");

        // Access properties
        Console.WriteLine($"\nAmount: {usdAmount.Amount}");
        Console.WriteLine($"Currency: {usdAmount.Currency}");

        // Arithmetic operations
        var total = usdAmount.Add(eurAmount).Add(gbpAmount);
        Console.WriteLine($"\nTotal of all amounts: {total}");

        var difference = usdAmount.Subtract(new Money(200.75m, "USD"));
        Console.WriteLine($"USD after subtraction: {difference}");

        // Comparison operations
        Console.WriteLine($"\nIs {usdAmount} > {eurAmount}? {usdAmount.IsGreaterThan(eurAmount)}");
        Console.WriteLine($"Is {gbpAmount} < {usdAmount}? {gbpAmount.IsLessThan(usdAmount)}");

        // Equality checks
        var sameAmount = new Money(1000.50m, "USD");
        Console.WriteLine($"\nAre amounts equal? {usdAmount.Equals(sameAmount)}");
        Console.WriteLine($"Hash codes match? {usdAmount.GetHashCode() == sameAmount.GetHashCode()}");

        // Formatting
        Console.WriteLine($"\nFormatted: {usdAmount.ToString("C", null)}");
        Console.WriteLine($"Formatted with culture: {usdAmount.ToString("C", new System.Globalization.CultureInfo("fr-FR"))}");

        // Operator overloading
        var sum = usdAmount + eurAmount;
        Console.WriteLine($"\nUsing + operator: {sum}");

        var product = usdAmount * 2;
        Console.WriteLine($"Using * operator: {product}");
    }

    public void MoneyInDomainOperations()
    {
        // Simulate domain operations with Money
        var initialBalance = new Money(1000.00m, "USD");
        var deposit = new Money(500.50m, "USD");
        var withdrawal = new Money(200.75m, "USD");

        // Calculate new balance
        var balanceAfterDeposit = initialBalance.Add(deposit);
        Console.WriteLine($"Balance after deposit: {balanceAfterDeposit}");

        var finalBalance = balanceAfterDeposit.Subtract(withdrawal);
        Console.WriteLine($"Final balance: {finalBalance}");

        // Check if sufficient funds
        var transferAmount = new Money(300.00m, "USD");
        if (finalBalance.IsGreaterThan(transferAmount) || finalBalance.Equals(transferAmount))
        {
            Console.WriteLine("Sufficient funds for transfer");
        }
        else
        {
            Console.WriteLine("Insufficient funds for transfer");
        }
    }
}
```

## DeadLetterEntry

`DeadLetterEntry` represents a domain event that could not be processed by a projection runner after all retry attempts were exhausted. It captures the failed event, the projection that failed, the error message, and metadata about retry attempts. This type is used by the dead-letter store to track events that need manual intervention or reprocessing.

Example usage:

```csharp
using System;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.ReadModels;

public class DeadLetterExample
{
    public async Task CreateDeadLetterEntry()
    {
        // Create a domain event that failed processing
        var failedEvent = new MoneyDeposited("agg-123", "ACC-0001", 1000.50m, "USD", "DEP-001")
        {
            CorrelationId = "corr-456",
            Timestamp = DateTime.UtcNow
        };

        // Create a dead-letter entry for the failed event
        var deadLetter = new DeadLetterEntry
        {
            Event = failedEvent,
            ProjectionName = "AccountReadModel",
            ErrorMessage = "Concurrency conflict: event version already processed",
            AttemptCount = 3,
            FailedAt = DateTime.UtcNow
        };

        Console.WriteLine($"Created dead-letter entry: {deadLetter.Id}");
        Console.WriteLine($"Failed projection: {deadLetter.ProjectionName}");
        Console.WriteLine($"Error: {deadLetter.ErrorMessage}");
        Console.WriteLine($"Attempts: {deadLetter.AttemptCount}");
        Console.WriteLine($"Failed at: {deadLetter.FailedAt:u}");

        // Mark as reprocessed when successfully handled
        deadLetter.MarkReprocessed();
        Console.WriteLine($"Reprocessed at: {deadLetter.ReprocessedAt?.ToString("u") ?? "null"}");
    }

    public async Task ProcessDeadLetterQueue(IDeadLetterStore store)
    {
        // Retrieve all unprocessed dead-letter entries for a projection
        var entries = await store.GetByProjectionAsync("AccountReadModel");
        
        foreach (var entry in entries)
        {
            Console.WriteLine($"Processing dead-letter: {entry.Id}");
            Console.WriteLine($"Event: {entry.Event.GetType().Name}");
            Console.WriteLine($"Error: {entry.ErrorMessage}");
            
            // Attempt to reprocess the event
            var success = await TryReprocessEvent(entry.Event);
            
            if (success)
            {
                // Mark as successfully reprocessed
                await store.MarkReprocessedAsync(entry.Id);
                Console.WriteLine("Successfully reprocessed dead-letter entry");
            }
        }
    }

    private async Task<bool> TryReprocessEvent(DomainEvent @event)
    {
        // Implementation-specific reprocessing logic
        await Task.Delay(100); // Simulate work
        return true;
    }
}
```

## InMemoryDeadLetterStore

`InMemoryDeadLetterStore` is a thread-safe, in-memory implementation of `IDeadLetterStore` that stores failed projection events for later reprocessing. It's designed for development and testing scenarios where persistence requirements are minimal. For production workloads, replace this with a durable store implementation.

The store provides basic CRUD operations for dead-letter entries including querying by projection name, aggregate ID, or retrieving all entries. It also supports marking entries as reprocessed and counting active dead-letter entries.

**Public members:**
- `WriteAsync(DeadLetterEntry entry, CancellationToken cancellationToken)` - Writes a dead-letter entry to the store
- `GetByProjectionAsync(string projectionName, CancellationToken cancellationToken)` - Gets all unprocessed dead-letter entries for a specific projection
- `GetByAggregateAsync(string aggregateId, CancellationToken cancellationToken)` - Gets all unprocessed dead-letter entries for a specific aggregate
- `GetAllAsync(bool includeReprocessed, CancellationToken cancellationToken)` - Gets all dead-letter entries, optionally including reprocessed ones
- `MarkReprocessedAsync(string entryId, CancellationToken cancellationToken)` - Marks a dead-letter entry as successfully reprocessed
- `GetCountAsync(CancellationToken cancellationToken)` - Gets the count of unprocessed dead-letter entries

Example usage:

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.ReadModels;
using Microsoft.Extensions.Logging;

public class InMemoryDeadLetterStoreExample
{
    private readonly InMemoryDeadLetterStore _store;
    
    public InMemoryDeadLetterStoreExample()
    {
        // Create store with logger (typically injected in real applications)
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<InMemoryDeadLetterStore>();
        _store = new InMemoryDeadLetterStore(logger);
    }
    
    public async Task ManageDeadLetterEntries()
    {
        // Create a failed domain event
        var failedEvent = new MoneyDeposited("agg-123", "ACC-0001", 1000.50m, "USD", "DEP-001")
        {
            CorrelationId = "corr-456",
            Timestamp = DateTime.UtcNow
        };
        
        // Create a dead-letter entry
        var deadLetter = new DeadLetterEntry
        {
            Event = failedEvent,
            ProjectionName = "AccountReadModel",
            ErrorMessage = "Concurrency conflict: event version already processed",
            AttemptCount = 3,
            FailedAt = DateTime.UtcNow
        };
        
        // Write the dead-letter entry
        await _store.WriteAsync(deadLetter);
        Console.WriteLine($"Dead-letter entry created: {deadLetter.Id}");
        
        // Get all unprocessed dead-letter entries for a projection
        var projectionEntries = await _store.GetByProjectionAsync("AccountReadModel");
        Console.WriteLine($"Found {projectionEntries.Count} entries for AccountReadModel projection");
        
        // Get all unprocessed dead-letter entries for an aggregate
        var aggregateEntries = await _store.GetByAggregateAsync("agg-123");
        Console.WriteLine($"Found {aggregateEntries.Count} entries for aggregate agg-123");
        
        // Get total count of unprocessed entries
        var count = await _store.GetCountAsync();
        Console.WriteLine($"Total unprocessed dead-letter entries: {count}");
        
        // Mark entry as reprocessed when successfully handled
        if (projectionEntries.Any())
        {
            var entryId = projectionEntries.First().Id;
            var markResult = await _store.MarkReprocessedAsync(entryId);
            if (markResult.IsSuccess)
            {
                Console.WriteLine($"Successfully marked entry {entryId} as reprocessed");
            }
        }
        
        // Get all entries including reprocessed ones
        var allEntries = await _store.GetAllAsync(includeReprocessed: true);
        Console.WriteLine($"Total entries (including reprocessed): {allEntries.Count}");
    }
}
```

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

## ReadModelProjectionOptions

`ReadModelProjectionOptions` configures the behavior of the `ReadModelProjectionEngine`, allowing fine-grained control over retry policies, checkpointing, concurrency, timeouts, and dead-letter handling. These options can be bound from configuration files or supplied programmatically when registering projection services.

Example usage:

```csharp
using System;
using DotNetCqrsEventSourcing.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class ReadModelProjectionOptionsExample
{
    public void ConfigureProjectionOptions()
    {
        // Example 1: Configure via Options pattern
        var services = new ServiceCollection();
        
        services.Configure<ReadModelProjectionOptions>(options =>
        {
            options.MaxRetryAttempts = 5;
            options.RetryBaseDelayMilliseconds = 200;
            options.EnableCheckpointing = true;
            options.CheckpointInterval = 25;
            options.MaxConcurrentProjectors = 8;
            options.ProjectorTimeout = TimeSpan.FromSeconds(60);
            options.ClearCheckpointsBeforeRebuild = false;
            options.EnableDeadLetterStore = true;
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<ReadModelProjectionOptions>>().Value;
        
        Console.WriteLine($"Max retry attempts: {options.MaxRetryAttempts}");
        Console.WriteLine($"Retry base delay: {options.RetryBaseDelayMilliseconds}ms");
        Console.WriteLine($"Checkpoint interval: {options.CheckpointInterval}");
        Console.WriteLine($"Max concurrent projectors: {options.MaxConcurrentProjectors}");
        Console.WriteLine($"Projector timeout: {options.ProjectorTimeout}");
    }
    
    public void UseDefaultOptions()
    {
        // Example 2: Use default options (all properties have sensible defaults)
        var defaultOptions = new ReadModelProjectionOptions();
        
        Console.WriteLine("Default options:");
        Console.WriteLine($"MaxRetryAttempts: {defaultOptions.MaxRetryAttempts}");
        Console.WriteLine($"RetryBaseDelayMilliseconds: {defaultOptions.RetryBaseDelayMilliseconds}");
        Console.WriteLine($"EnableCheckpointing: {defaultOptions.EnableCheckpointing}");
        Console.WriteLine($"CheckpointInterval: {defaultOptions.CheckpointInterval}");
        Console.WriteLine($"MaxConcurrentProjectors: {defaultOptions.MaxConcurrentProjectors}");
        Console.WriteLine($"ProjectorTimeout: {defaultOptions.ProjectorTimeout}");
        Console.WriteLine($"ClearCheckpointsBeforeRebuild: {defaultOptions.ClearCheckpointsBeforeRebuild}");
        Console.WriteLine($"EnableDeadLetterStore: {defaultOptions.EnableDeadLetterStore}");
    }
    
    public void ConfigureFromConfiguration()
    {
        // Example 3: Bind from configuration (appsettings.json)
        // Configuration structure:
        /*
        {
          "ReadModelProjections": {
            "MaxRetryAttempts": 5,
            "RetryBaseDelayMilliseconds": 200,
            "EnableCheckpointing": true,
            "CheckpointInterval": 25,
            "MaxConcurrentProjectors": 8,
            "ProjectorTimeout": "00:01:00",
            "ClearCheckpointsBeforeRebuild": false,
            "EnableDeadLetterStore": true
          }
        }
        */
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