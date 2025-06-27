# EventStoreBenchmarks

This class provides benchmarking utilities for measuring the performance of event sourcing operations using EventStoreDB. It evaluates core operations such as appending events, replaying aggregates, and querying event streams, enabling comparison of different event sourcing patterns and infrastructure configurations.

## API

### `public void GlobalSetup()`

Initializes the benchmark environment before any tests are executed. This method sets up the necessary infrastructure, including the EventStoreDB connection, test event streams, and any required aggregate state. It is called once per benchmark run.

- **Parameters**: None
- **Return value**: None
- **Throws**: May throw if EventStoreDB connection fails or test data cannot be initialized.

---

### `public void GlobalCleanup()`

Cleans up resources allocated during `GlobalSetup()`. This includes closing the EventStoreDB connection, disposing of test aggregates, and clearing temporary data. It is called once after all benchmarks have completed.

- **Parameters**: None
- **Return value**: None
- **Throws**: May throw if cleanup operations encounter unrecoverable errors (e.g., connection already closed).

---

### `public async Task EventStore_AppendSingleEvent()`

Benchmarks the performance of appending a single event to an aggregate stream in EventStoreDB. Measures latency and throughput for a minimal write operation.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if the event append operation fails (e.g., stream not found, connection issues).

---

### `public async Task EventStore_AppendBatchOf100Events()`

Benchmarks the performance of appending a batch of 100 events to an aggregate stream in EventStoreDB. Evaluates bulk write efficiency and latency under moderate load.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if batch append fails (e.g., concurrency conflicts, validation errors).

---
### `public async Task AggregateRoot_Replay100Events()`

Benchmarks the replay of 100 events to reconstruct an aggregate root state. Measures the time taken to apply events sequentially and rebuild the aggregate’s internal state.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if event replay encounters an unhandled exception (e.g., missing event handler, corrupted event data).

---
### `public async Task AggregateRoot_Replay1000Events()`

Benchmarks the replay of 1,000 events to reconstruct an aggregate root state. Evaluates performance under increased event volume, simulating long-lived aggregates.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if event replay fails due to excessive memory usage or event handler errors.

---
### `public async Task AggregateRoot_Replay10000Events()`

Benchmarks the replay of 10,000 events to reconstruct an aggregate root state. Tests scalability and resource consumption for aggregates with very large event histories.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if event replay exceeds memory limits or encounters unhandled exceptions.

---
### `public async Task EventStore_GetEventsByAggregateId()`

Benchmarks the retrieval of all events for a given aggregate ID from EventStoreDB. Measures latency and throughput for a read-heavy operation.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if the query fails (e.g., stream not found, permission denied).

---
### `public async Task EventStore_GetEventsFromVersion()`

Benchmarks the retrieval of events from a specific version onward for a given aggregate ID. Evaluates performance of partial stream reads, common in event sourcing.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if the query fails (e.g., invalid version, stream not found).

---
### `public async Task EventStore_GetAggregateVersion()`

Benchmarks the retrieval of the current version of an aggregate stream from EventStoreDB. Measures the cost of a lightweight metadata query.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if the stream does not exist or access is denied.

---
### `public async Task AccountService_CompleteLifecycle()`

Benchmarks a full lifecycle of an account aggregate, including creation, event appending, and state replay. Simulates a realistic business flow to evaluate end-to-end performance.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if any step in the lifecycle fails (e.g., event validation, concurrency conflict).

---
### `public async Task AccountService_CreateAccount()`

Benchmarks the creation of a new account aggregate, including initial event appending and state initialization. Measures the overhead of aggregate creation in the event sourcing model.

- **Parameters**: None
- **Return value**: `Task` representing the asynchronous operation.
- **Throws**: May throw if account creation fails (e.g., duplicate ID, validation error).

## Usage

### Example 1: Running a single benchmark
