# CompactionResultExtensions

Provides extension methods for the `CompactionResult` type to facilitate common operations when working with event-sourced aggregates, particularly in scenarios involving event compaction or version adjustments.

## API

### `WithAdditionalEventsRemoved`
Creates a new `CompactionResult` with the specified events removed from the compaction result.

- **Parameters**
  - `result`: The original `CompactionResult` instance.
  - `eventIdsToRemove`: A collection of event identifiers to exclude from the compaction result.
- **Return Value**
  Returns a new `CompactionResult` containing only the events not specified for removal.
- **Exceptions**
  Throws `ArgumentNullException` if `result` or `eventIdsToRemove` is `null`.

### `WithVersionDelta`
Adjusts the version of a `CompactionResult` by applying a delta value.

- **Parameters**
  - `result`: The original `CompactionResult` instance.
  - `delta`: The integer value to add to the current version.
- **Return Value**
  Returns a new `CompactionResult` with the version updated by the specified delta.
- **Exceptions**
  Throws `ArgumentNullException` if `result` is `null`.
  Throws `OverflowException` if the resulting version exceeds the bounds of an `int`.

### `IsNoOp`
Determines whether the compaction operation represented by the `CompactionResult` is a no-op (i.e., no changes were made).

- **Parameters**
  - `result`: The `CompactionResult` instance to evaluate.
- **Return Value**
  Returns `true` if the compaction did not alter the event stream; otherwise, `false`.
- **Exceptions**
  Throws `ArgumentNullException` if `result` is `null`.

### `ToDetailedString`
Converts the `CompactionResult` into a human-readable diagnostic string.

- **Parameters**
  - `result`: The `CompactionResult` instance to convert.
- **Return Value**
  Returns a formatted string containing version, event count, and other relevant details.
- **Exceptions**
  Throws `ArgumentNullException` if `result` is `null`.

## Usage
