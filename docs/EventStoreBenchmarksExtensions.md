# EventStoreBenchmarksExtensions

Utility class providing benchmarking capabilities for EventStoreDB operations in the dotnet-cqrs-eventsourcing project. Designed to measure performance characteristics of event sourcing patterns, aggregate root operations, and service layer interactions with configurable parameters.

## API

### `WithCustomParameters`

Configures the benchmarking environment with custom parameters before execution. This method must be called prior to any benchmark execution to override default settings.

- **Parameters**
  - `parameters`: `Dictionary<string, object>` – Key-value pairs representing custom benchmarking parameters (e.g., event batch size, concurrency level, connection timeout).
- **Return Value**: `EventStoreBenchmarks` – Returns the configured `EventStoreBenchmarks` instance for method chaining.
- **Throws**: `ArgumentNullException` – If `parameters` is `null`.

### `RunEventStoreBenchmarksAsync`

Executes a suite of benchmarks targeting core EventStoreDB operations such as append, read, and stream operations. Measures latency, throughput, and resource utilization under specified conditions.

- **Parameters**: None
- **Return Value**: `Task<Dictionary<string, object>>` – A dictionary containing benchmark results with keys such as `"latencyMs"`, `"opsPerSec"`, and `"errors"`.
- **Throws**:
  - `InvalidOperationException` – If `WithCustomParameters` was not called prior to execution.
  - `EventStoreConnectionException` – If the EventStoreDB connection cannot be established.
  - `BenchmarkExecutionException` – If an error occurs during benchmark execution.

### `RunAggregateRootBenchmarksAsync`

Runs benchmarks focused on aggregate root operations including event application, snapshot generation, and command handling. Validates consistency and performance under load.

- **Parameters**: None
- **Return Value**: `Task<Dictionary<string, object>>` – A dictionary containing aggregate-specific metrics like `"eventsAppliedPerSec"`, `"snapshotSize"`, and `"consistencyErrors"`.
- **Throws**:
  - `InvalidOperationException` – If `WithCustomParameters` was not called prior to execution.
  - `AggregateException` – If an error occurs during aggregate processing.

### `RunAccountServiceBenchmarksAsync`

Benchmarks the account service layer, measuring command processing, event publishing, and read model updates. Useful for validating service-level performance and scalability.

- **Parameters**: None
- **Return Value**: `Task<Dictionary<string, object>>` – A dictionary with service metrics such as `"commandsPerSec"`, `"eventPublishLatency"`, and `"readModelSyncTime"`.
- **Throws**:
  - `InvalidOperationException` – If `WithCustomParameters` was not called prior to execution.
  - `ServiceException` – If the account service fails during benchmark execution.

### `DisposeServiceProvider`

Releases resources associated with the benchmarking environment, including the service provider and EventStoreDB connection. Must be called after benchmark execution to avoid resource leaks.

- **Parameters**: None
- **Return Value**: `void`
- **Throws**: None

## Usage

### Basic Benchmark Execution
