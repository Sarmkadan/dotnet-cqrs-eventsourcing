# EventStoreCompactionServiceTests

Unit tests for the `EventStoreCompactionService` class, verifying compaction behavior for event-sourced aggregates. These tests validate event removal logic, snapshot handling, version validation, and aggregate skipping during bulk compaction operations.

## API

### `EventStoreCompactionServiceTests`
Public test class containing test cases for event store compaction functionality. Serves as the container for verifying compaction behavior under various conditions.

### `CompactToVersionAsync_RemovesEventsBeforeVersion`
Verifies that the compaction service correctly removes all events prior to the specified version from the event store. Ensures that only events at or after the target version remain.

- **Parameters**: None (test method)
- **Return value**: `Task` (async test completion)
- **Throws**: Standard test assertion exceptions on failure

### `CompactAsync_WithSnapshot_UsesSnapshotVersion`
Validates that when a snapshot exists, the compaction service uses the snapshot's version as the starting point for compaction rather than the specified version. Ensures snapshot consistency is preserved.

- **Parameters**: None (test method)
- **Return value**: `Task` (async test completion)
- **Throws**: Standard test assertion exceptions on failure

### `CompactAsync_NoSnapshot_ReturnsFailure`
Confirms that compaction fails when no snapshot exists for an aggregate. Ensures the service enforces snapshot requirements for compaction operations.

- **Parameters**: None (test method)
- **Return value**: `Task` (async test completion)
- **Throws**: Standard test assertion exceptions on failure

### `CompactAllAsync_SkipsAggregatesWithoutSnapshots`
Ensures that during bulk compaction, aggregates without snapshots are skipped entirely. Validates that the compaction process handles missing snapshots gracefully without failing the entire operation.

- **Parameters**: None (test method)
- **Return value**: `Task` (async test completion)
- **Throws**: Standard test assertion exceptions on failure

### `CompactToVersionAsync_InvalidVersion_ReturnsFailure`
Verifies that the compaction service returns a failure result when an invalid (e.g., negative or excessively high) version is provided. Ensures input validation is enforced.

- **Parameters**: None (test method)
- **Return value**: `Task` (async test completion)
- **Throws**: Standard test assertion exceptions on failure

## Usage
