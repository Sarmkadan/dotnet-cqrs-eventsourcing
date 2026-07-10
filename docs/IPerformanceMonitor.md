# IPerformanceMonitor

`IPerformanceMonitor` is an abstraction designed to track, aggregate, and report performance metrics for operations within a CQRS and Event Sourcing system. It allows developers to monitor execution duration, success and failure rates, and invocation counts, providing essential telemetry for diagnosing bottlenecks and assessing system stability across high-concurrency environments.

## API

### IPerformanceMonitor

The following members define the interface for monitoring operation performance.

*   `string OperationName`: Gets the name of the operation being monitored.
*   `long InvocationCount`: Gets the total number of times the operation has been invoked.
*   `long FailureCount`: Gets the total number of recorded failures for the operation.
*   `long TotalDurationMs`: Gets the total duration, in milliseconds, of all operation invocations.
*   `long MinDurationMs`: Gets the minimum duration recorded for a single invocation of the operation, in milliseconds.
*   `long MaxDurationMs`: Gets the maximum duration recorded for a single invocation of the operation, in milliseconds.
*   `DateTime LastInvokedAt`: Gets the timestamp of the most recent invocation.
*   `void RecordOperation(string name, long durationMs, bool success)`: Records the outcome and duration of an operation.
*   `OperationStatistics? GetStatistics(string name)`: Retrieves statistics for a specific operation by name. Returns `null` if no statistics are found for the given name.
*   `IEnumerable<(string Name, OperationStatistics Stats)> GetAllStatistics()`: Retrieves all tracked statistics, returning a collection of tuples containing the operation name and its corresponding `OperationStatistics`.
*   `void Clear()`: Resets all tracked metrics to their initial state.

### OperationStatistics

This class holds aggregated metrics for a specific operation.

*   `long InvocationCount`: The total number of times the operation has been invoked.
*   `long SuccessCount`: The total number of successful invocations.
*   `long FailureCount`: The total number of recorded failures.
*   `double SuccessRate`: The calculated ratio of successful invocations to total invocations.
*   `double AverageDurationMs`: The average duration of all invocations, in milliseconds.
*   `long MinDurationMs`: The minimum duration recorded for a single invocation, in milliseconds.
*   `long MaxDurationMs`: The maximum duration recorded for a single invocation, in milliseconds.
*   `DateTime LastInvokedAt`: The timestamp of the most recent invocation.

### Implementation

*   `PerformanceMonitor()`: Constructor for the concrete implementation of `IPerformanceMonitor`.

## Usage

### Recording Operation Metrics
```csharp
// Example: Recording an operation after execution
public async Task HandleAsync(MyCommand command)
{
    var stopwatch = Stopwatch.StartNew();
    bool success = false;
    try
    {
        await _commandHandler.ExecuteAsync(command);
        success = true;
    }
    finally
    {
        stopwatch.Stop();
        _performanceMonitor.RecordOperation(nameof(MyCommand), stopwatch.ElapsedMilliseconds, success);
    }
}
```

### Inspecting Performance Metrics
```csharp
// Example: Retrieving and logging performance statistics
public void LogPerformanceReport()
{
    var stats = _performanceMonitor.GetAllStatistics();
    foreach (var (name, stat) in stats)
    {
        Console.WriteLine($"Operation: {name}");
        Console.WriteLine($"  Total: {stat.InvocationCount}, Success Rate: {stat.SuccessRate:P2}");
        Console.WriteLine($"  Avg Duration: {stat.AverageDurationMs}ms");
    }
}
```

## Notes

*   **Thread Safety:** Implementations of `IPerformanceMonitor` are expected to be thread-safe, as they are typically used in environments with concurrent command or event processing. Metrics aggregation should be performed using atomic operations or thread-safe primitives to ensure data integrity.
*   **State Reset:** The `Clear` method is destructive and will erase all historical performance data. This should be used cautiously, typically only during system initialization or when performance monitoring windows are explicitly rotated.
*   **Edge Cases:**
    *   Before the first invocation of an operation, statistics properties may return default values (e.g., zero for counts, `DateTime.MinValue` for `LastInvokedAt`).
    *   If `RecordOperation` is called with a duration of zero, it will affect the average and total duration calculations accordingly.
    *   `GetStatistics` returns `null` if the specified operation name has not been recorded, necessitating null checks by the caller.
