# AccountAggregateTests

The `AccountAggregateTests` class serves as the comprehensive test suite for the `Account` domain aggregate within the `dotnet-cqrs-eventsourcing` project. It validates the core business logic, state transitions, and event generation capabilities of the banking account entity, ensuring that operations such as creation, deposits, withdrawals, and account closure adhere strictly to defined domain rules and correctly emit corresponding domain events for the event sourcing infrastructure.

## API

### `CreateAccount_ValidParameters_RaisesAccountCreatedEvent`
Verifies that initializing an account with valid parameters successfully raises an `AccountCreated` domain event. This method takes no parameters and returns `void`. It does not throw exceptions under normal test execution; failure results in an assertion error.

### `CreateAccount_ValidParameters_SetsAccountPropertiesCorrectly`
Confirms that upon successful creation with valid inputs, the internal state of the account (such as account number and holder name) is populated correctly. This method takes no parameters and returns `void`. It does not throw exceptions; failure results in an assertion error.

### `CreateAccount_AlreadyCreated_ThrowsDomainException`
Ensures that attempting to re-initialize an account that has already been created results in a `DomainException`. This method takes no parameters and returns `void`. It expects the target operation to throw a `DomainException`; if no exception or a different exception type is thrown, the test fails.

### `CreateAccount_EmptyAccountNumber_ThrowsDomainException`
Validates that the creation logic rejects requests where the account number is empty or null, throwing a `DomainException`. This method takes no parameters and returns `void`. It expects a `DomainException` to be thrown during the invocation of the creation logic with invalid data.

### `CreateAccount_EmptyAccountHolder_ThrowsDomainException`
Validates that the creation logic rejects requests where the account holder's name is empty or null, throwing a `DomainException`. This method takes no parameters and returns `void`. It expects a `DomainException` to be thrown during the invocation of the creation logic with invalid data.

### `Deposit_PositiveAmount_IncreasesBalanceAndRecordsTransaction`
Tests that depositing a positive amount correctly increases the account balance and appends a transaction record (and associated domain event). This method takes no parameters and returns `void`. It does not throw exceptions; failure results in an assertion error regarding balance calculation or event recording.

### `Deposit_ZeroAmount_ThrowsDomainException`
Ensures that attempting to deposit an amount of zero results in a `DomainException`. This method takes no parameters and returns `void`. It expects the deposit operation to throw a `DomainException`.

### `Deposit_NegativeAmount_ThrowsDomainException`
Ensures that attempting to deposit a negative amount results in a `DomainException`. This method takes no parameters and returns `void`. It expects the deposit operation to throw a `DomainException`.

### `Deposit_ClosedAccount_ThrowsDomainException`
Validates that any attempt to deposit funds into an account that has already been closed results in a `DomainException`. This method takes no parameters and returns `void`. It expects the deposit operation to throw a `DomainException`.

### `Withdraw_SufficientFunds_DecreasesBalanceAndRecordsTransaction`
Tests that withdrawing an amount within the available balance correctly decreases the account balance and records the transaction. This method takes no parameters and returns `void`. It does not throw exceptions; failure results in an assertion error.

### `Withdraw_InsufficientFunds_ThrowsDomainException`
Ensures that attempting to withdraw more funds than are available in the account results in a `DomainException`. This method takes no parameters and returns `void`. It expects the withdraw operation to throw a `DomainException`.

### `Withdraw_ZeroAmount_ThrowsDomainException`
Ensures that attempting to withdraw an amount of zero results in a `DomainException`. This method takes no parameters and returns `void`. It expects the withdraw operation to throw a `DomainException`.

### `CloseAccount_ActiveAccount_SetsStatusToClosedAndRaisesEvent`
Verifies that closing an active account updates its status to "Closed" and raises the appropriate domain event. This method takes no parameters and returns `void`. It does not throw exceptions; failure results in an assertion error.

### `CloseAccount_AlreadyClosedAccount_ThrowsDomainException`
Ensures that attempting to close an account that is already in a closed state results in a `DomainException`. This method takes no parameters and returns `void`. It expects the close operation to throw a `DomainException`.

### `LoadFromHistory_RebuildsAccountStateFromEvents`
Validates the event sourcing reconstruction logic by applying a sequence of historical events to a new instance and asserting that the final state matches the expected outcome. This method takes no parameters and returns `void`. It does not throw exceptions; failure results in an assertion error regarding state reconstruction.

### `ClearUncommittedEvents_AfterRaisingEvents_EmptiesQueue`
Confirms that invoking the mechanism to clear uncommitted events successfully empties the internal event queue after events have been raised. This method takes no parameters and returns `void`. It does not throw exceptions; failure results in an assertion error.

## Usage

### Example 1: Verifying State Transition and Event Generation
This example demonstrates how the test suite validates that a deposit operation not only updates the balance but also queues the correct domain event.

```csharp
[TestFixture]
public class AccountBehaviorSpecs
{
    [Test]
    public void VerifyDepositWorkflow()
    {
        // Arrange
        var tests = new AccountAggregateTests();
        
        // Act & Assert
        // The test method internally instantiates an account, performs the deposit,
        // and asserts both the balance change and the event presence.
        tests.Deposit_PositiveAmount_IncreasesBalanceAndRecordsTransaction();
        
        // If the method completes without throwing an assertion exception,
        // the account balance was increased and the transaction event was recorded.
    }
}
```

### Example 2: Validating Domain Exception Handling
This example illustrates testing the protective guards of the aggregate, specifically ensuring that business rules prevent invalid operations like withdrawing from a closed account.

```csharp
[TestFixture]
public class AccountGuardSpecs
{
    [Test]
    public void VerifyClosedAccountRestrictions()
    {
        // Arrange
        var tests = new AccountAggregateTests();
        
        // Act & Assert
        // This test method internally creates an account, closes it,
        // attempts a deposit, and verifies that a DomainException is thrown.
        tests.Deposit_ClosedAccount_ThrowsDomainException();
        
        // Successful completion confirms the aggregate enforces the "No deposits on closed accounts" rule.
    }
}
```

## Notes

*   **Exception Consistency**: All validation failures within the `Account` aggregate (e.g., negative amounts, insufficient funds, invalid states) consistently throw `DomainException`. Tests relying on specific exception types other than `DomainException` will fail.
*   **State Dependency**: Several tests (e.g., `Deposit_ClosedAccount_ThrowsDomainException`, `CloseAccount_AlreadyClosedAccount_ThrowsDomainException`) rely on specific prior states of the aggregate. The test implementation must ensure the aggregate is correctly transitioned to these prerequisite states before invoking the target method.
*   **Event Sourcing Integrity**: The `LoadFromHistory_RebuildsAccountStateFromEvents` test is critical for the event sourcing pattern. It ensures that the `Apply` methods for all events are implemented correctly and that the order of events in the history stream is respected during reconstruction.
*   **Thread Safety**: As with most domain aggregate test suites, these tests are designed to run in isolation. The `Account` aggregate itself is typically not thread-safe and assumes single-threaded access within a transactional context. These tests do not perform concurrent access checks; running individual test methods in parallel against the same instance is not supported and may lead to race conditions.
*   **Side Effects**: Methods ending in `ThrowsDomainException` verify the *absence* of side effects (such as balance changes or event recording) when an exception occurs, ensuring the aggregate remains in a consistent state even after a failed operation.
