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
