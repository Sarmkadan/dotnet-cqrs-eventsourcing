# ReadModelRebuilderCommandTests

Unit test suite for the `ReadModelRebuilderCommand` handler, verifying command naming, execution paths for rebuild operations (including dry-run and aggregate-specific modes), failure propagation from the projection service, and integration with the command dispatcher for both known and unknown command types.

## API

### ReadModelRebuilderCommandTests

Constructor. Initializes the test context, including mocked dependencies such as the projection service and command dispatcher, before each test case executes.

### void Name_IsExpectedValue

Asserts that the `Name` property of `ReadModelRebuilderCommand` returns the expected, hard-coded command name string. No parameters. No return value. Does not throw.

### async Task ExecuteAsync_NoArgs_ReturnsFailureWithMissingArgument

Verifies that invoking `ExecuteAsync` with an empty argument list produces a failure result containing a "missing argument" error message. No parameters. Returns a task representing the asynchronous test operation. Does not throw.

### async Task ExecuteAsync_AllFlag_CallsRebuildAllProjectionsAsync

Confirms that passing the `--all` flag to `ExecuteAsync` triggers a call to `RebuildAllProjectionsAsync` on the projection service. No parameters. Returns a task. Does not throw.

### async Task ExecuteAsync_AggregateFlag_CallsRebuildProjectionForSpecificAggregate

Ensures that supplying an aggregate identifier argument to `ExecuteAsync` results in a call to `RebuildProjectionForSpecificAggregate` with the matching identifier. No parameters. Returns a task. Does not throw.

### async Task ExecuteAsync_DryRunWithAllFlag_SkipsRebuildAndSucceeds

Validates that combining the `--dry-run` flag with `--all` causes the handler to skip any actual rebuild invocation and return a success result. No parameters. Returns a task. Does not throw.

### async Task ExecuteAsync_ProjectionServiceFails_PropagatesFailure

Simulates a failure thrown by the projection service during rebuild and asserts that the resulting failure is propagated unchanged through the command result. No parameters. Returns a task. Does not throw.

### async Task DispatchAsync_UnknownCommand_ReturnsFailure

Tests the dispatcher with an unrecognized command type and expects a failure result indicating the command is unknown. No parameters. Returns a task. Does not throw.

### async Task DispatchAsync_KnownCommand_ExecutesAndReturnsSuccess

Dispatches a `ReadModelRebuilderCommand` instance through the dispatcher and verifies that it executes successfully, returning a success result. No parameters. Returns a task. Does not throw.

## Usage

```csharp
// Example 1: Testing rebuild-all behavior with mocked projection service
[Fact]
public async Task RebuildAll_ShouldInvokeProjectionService()
{
    var mockProjectionService = new Mock<IProjectionService>();
    var handler = new ReadModelRebuilderCommand(mockProjectionService.Object);

    var result = await handler.ExecuteAsync(new[] { "--all" });

    Assert.True(result.IsSuccess);
    mockProjectionService.Verify(
        ps => ps.RebuildAllProjectionsAsync(It.IsAny<CancellationToken>()),
        Times.Once);
}
```

```csharp
// Example 2: Testing failure propagation when projection service throws
[Fact]
public async Task Rebuild_WhenServiceThrows_ShouldReturnFailure()
{
    var mockProjectionService = new Mock<IProjectionService>();
    mockProjectionService
        .Setup(ps => ps.RebuildAllProjectionsAsync(It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("Connection lost"));

    var handler = new ReadModelRebuilderCommand(mockProjectionService.Object);

    var result = await handler.ExecuteAsync(new[] { "--all" });

    Assert.False(result.IsSuccess);
    Assert.Contains("Connection lost", result.ErrorMessage);
}
```

## Notes

- **Argument parsing**: The `ExecuteAsync` method distinguishes modes based on the presence of flags (`--all`, `--dry-run`) and positional aggregate identifiers. Tests assume case-sensitive, exact-match argument strings.
- **Failure propagation**: When the projection service throws an exception, the handler wraps it in a failure result without altering the exception message or type. Tests do not assert on exception stack traces.
- **Dispatcher integration**: The dispatcher tests rely on a pre-configured mapping of command names to handler types. An unknown command name yields failure; a known name resolves, instantiates, and executes the handler.
- **Thread safety**: These tests are synchronous from the perspective of shared state—each test method creates its own mocked dependencies and handler instance. No static state or shared fixtures are mutated, so parallel test execution is safe without additional synchronization.
