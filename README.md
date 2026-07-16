// existing content ...

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
