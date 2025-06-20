# AccountServiceTests

Test class that verifies the behavior of the `AccountService` implementation within the CQRS/event‑sourcing domain. Each public async method represents a single unit test exercising a specific scenario: successful command handling, failure paths due to validation, domain rule violations, or repository errors, and the correct publishing of events.

## API

| Member | Purpose | Parameters | Return Value | Throws |
|--------|---------|------------|--------------|--------|
| `CreateAccountAsync_ValidParameters_ReturnsSuccessWithAccount` | Confirms that creating an account with valid input returns a success result containing the newly created account. | none | `Task` (completes when the test finishes) | May throw an exception if the test framework’s assertions fail (e.g., `Xunit.Assert` exceptions). |
| `CreateAccountAsync_RepositorySaveFails_ReturnsFailure` | Verifies that when the underlying repository throws during the save operation, the service returns a failure result without creating an account. | none | `Task` | May throw an exception if assertions fail. |
| `CreateAccountAsync_InvalidDomainOperation_ReturnsFailureWithCode` | Ensures that a domain rule violation (e.g., attempting to create an account with an already‑existing identifier) results in a failure result carrying the appropriate error code. | none | `Task` | May throw an exception if assertions fail. |
| `DepositAsync_AccountNotFound_ReturnsFailure` | Checks that depositing to a non‑existent account yields a failure result indicating the account could not be found. | none | `Task` | May throw an exception if assertions fail. |
| `DepositAsync_ValidAccount_SavesAndPublishesEvents` | Validates that a successful deposit persists the updated account state via the repository and publishes the expected `MoneyDeposited` event. | none | `Task` | May throw an exception if assertions fail. |
| `WithdrawAsync_InsufficientFunds_ReturnsFailure` | Asserts that withdrawing more than the available balance results in a failure result with an insufficient‑funds error. | none | `Task` | May throw an exception if assertions fail. |
| `CloseAccountAsync_ValidAccount_SucceedsAndPublishesClosedEvent` | Confirms that closing an existing account succeeds and triggers publication of an `AccountClosed` event. | none | `Task` | May throw an exception if assertions fail. |
| `GetTransactionCountAsync_AfterDeposit_ReturnsCorrectCount` | Verifies that after performing a deposit, the transaction count retrieved from the service matches the expected increment. | none | `Task` | May throw an exception if assertions fail. |
| `CreateAccountAsync_InvalidCurrency_ReturnsFailure` | Ensures that supplying an unsupported or invalid currency code during account creation leads to a failure result. | none | `Task` | May throw an exception if assertions fail. |
| `GetAccountAsync_RepositoryThrowsException_ReturnsFailure` | Checks that when the repository throws an exception while fetching an account, the service translates this into a failure result. | none | `Task` | May throw an exception if assertions fail. |
| `WithdrawAsync_RepositoryThrowsException_ReturnsFailure` | Validates that a repository exception during a withdraw operation results in a failure result from the service. | none | `Task` | May throw an exception if assertions fail. |
| `DepositAsync_RepositoryThrowsException_ReturnsFailure` | Ensures that a repository exception while processing a deposit is propagated as a failure result. | none | `Task` | May throw an exception if assertions fail. |
| `CloseAccountAsync_RepositoryThrowsException_ReturnsFailure` | Confirms that a repository exception during account closure leads to a failure result. | none | `Task` | May throw an exception if assertions fail. |

## Usage

The test class is intended to be executed by a test runner (e.g., `dotnet test` or an IDE’s test explorer). No direct instantiation is required in production code.

```csharp
// Example 1: Running all tests in the class via the CLI
// From the repository root:
dotnet test --filter FullyQualifiedName~AccountServiceTests
```

```csharp
// Example 2: Selectively executing a single test method
dotnet test --filter FullyQualifiedName~AccountServiceTests.CreateAccountAsync_ValidParameters_ReturnsSuccessWithAccount
```

When debugging, you can set breakpoints inside any test method and launch the test session from Visual Studio or Rider to inspect the service’s interactions with mocked repositories and event publishers.

## Notes

- Each test method is independent; the class does not maintain mutable state between tests, making it safe to run tests in parallel.
- The tests rely on mocking frameworks (e.g., Moq) to simulate repository behavior and to capture published events; therefore, they are deterministic and do not depend on external resources.
- If the underlying `AccountService` implementation changes its error‑code scheme or event names, the corresponding tests will fail, providing immediate feedback.
- The test methods return `Task`; any unhandled exception will cause the test to be marked as failed by the test runner.
- No thread‑safety guarantees are required for the test class itself, as each test executes on its own thread or async context.
