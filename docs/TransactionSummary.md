# TransactionSummary
The `TransactionSummary` type is a sealed record designed to provide a comprehensive overview of a transaction, including account details, balance information, and transaction history. It serves as a data container, encapsulating various aspects of a transaction, making it easier to manage and analyze financial data.

## API
* `AccountId`: A required string representing the unique identifier of the account.
* `AccountNumber`: A required string representing the account number.
* `AccountHolder`: A required string representing the name of the account holder.
* `Currency`: A required string representing the currency of the account.
* `OpenedAt`: A required DateTime representing the date and time the account was opened.
* `CurrentBalance`: A decimal representing the current balance of the account.
* `Status`: An `AccountReadModelStatus` representing the current status of the account.
* `ClosedAt`: A nullable DateTime representing the date and time the account was closed, if applicable.
* `ClosureReason`: A nullable string representing the reason for account closure, if applicable.
* `Transactions`: A list of `TransactionSummary` objects representing the transaction history of the account.
* `TotalDeposited`: A decimal representing the total amount deposited into the account.
* `TotalWithdrawn`: A decimal representing the total amount withdrawn from the account.
* `ProjectedVersion`: A long representing the projected version of the account.
* `LastUpdatedAt`: A DateTime representing the date and time the account was last updated.

## Usage
The following examples demonstrate how to create and utilize `TransactionSummary` objects:
```csharp
// Example 1: Creating a new TransactionSummary
var transactionSummary = new TransactionSummary
{
    AccountId = "12345",
    AccountNumber = "1234567890",
    AccountHolder = "John Doe",
    Currency = "USD",
    OpenedAt = DateTime.Now,
    CurrentBalance = 1000.00m,
    Status = AccountReadModelStatus.Active,
    Transactions = new List<TransactionSummary>()
};

// Example 2: Updating an existing TransactionSummary
var existingTransactionSummary = new TransactionSummary
{
    AccountId = "12345",
    AccountNumber = "1234567890",
    AccountHolder = "John Doe",
    Currency = "USD",
    OpenedAt = DateTime.Now,
    CurrentBalance = 1000.00m,
    Status = AccountReadModelStatus.Active,
    Transactions = new List<TransactionSummary>()
};

existingTransactionSummary.CurrentBalance += 500.00m;
existingTransactionSummary.Transactions.Add(new TransactionSummary
{
    AccountId = "12345",
    AccountNumber = "1234567890",
    AccountHolder = "John Doe",
    Currency = "USD",
    OpenedAt = DateTime.Now,
    CurrentBalance = 500.00m,
    Status = AccountReadModelStatus.Active
});
```

## Notes
When working with `TransactionSummary` objects, consider the following:
* The `AccountId`, `AccountNumber`, `AccountHolder`, and `Currency` properties are required and must be provided when creating a new `TransactionSummary` object.
* The `OpenedAt` property represents the date and time the account was opened and should be set accordingly.
* The `CurrentBalance` property should be updated carefully to ensure accuracy and consistency.
* The `Transactions` list can grow large, so consider implementing pagination or other data management strategies when working with large datasets.
* The `ProjectedVersion` property is used for versioning and should be incremented accordingly to ensure data consistency.
* The `LastUpdatedAt` property should be updated whenever the `TransactionSummary` object is modified to ensure accurate tracking of changes.
* `TransactionSummary` objects are not thread-safe by default, so consider implementing synchronization mechanisms when working with multiple threads.
