# CompactionResult

`CompactionResult` represents the outcome of an event stream compaction operation in a CQRS event-sourcing system. It captures the identity of the aggregate whose events were compacted, the number of events removed during the process, the version the stream was compacted to, and the timestamp when compaction occurred. This type serves as an immutable record of a completed compaction, enabling auditing, logging, and monitoring of storage optimization operations.

## API

### public string AggregateId

Gets the unique identifier of the aggregate whose event stream was compacted.

**Type:** `string`  
**Access:** Read-only property  
**Throws:** Nothing (simple property access)

### public int EventsRemoved

Gets the count of events that were removed from the event stream during compaction.

**Type:** `int`  
**Access:** Read-only property  
**Throws:** Nothing (simple property access)

### public long CompactedToVersion

Gets the version number to which the event stream was compacted. All events up to and including this version were collapsed or removed, leaving the stream at this version boundary.

**Type:** `long`  
**Access:** Read-only property  
**Throws:** Nothing (simple property access)

### public DateTime CompactedAt

Gets the UTC timestamp at which the compaction operation was completed.

**Type:** `DateTime`  
**Access:** Read-only property  
**Throws:** Nothing (simple property access)

### public CompactionResult

Constructs a new `CompactionResult` instance with the specified compaction details.

**Parameters:**
- `string aggregateId` — The aggregate identifier.
- `int eventsRemoved` — The number of events removed.
- `long compactedToVersion` — The target version after compaction.
- `DateTime compactedAt` — The timestamp of compaction completion.

**Throws:** Nothing (constructor performs simple field assignment)

### public override string ToString

Returns a string representation of the compaction result, typically including the aggregate identifier, events removed, compacted-to version, and timestamp in a human-readable format.

**Return Value:** `string` — A formatted representation of the compaction result.  
**Throws:** Nothing

## Usage

### Example 1: Recording a Successful Compaction

```csharp
// After compacting an aggregate's event stream, capture the result
var compactionResult = new CompactionResult(
    aggregateId: "order-2024-0991",
    eventsRemoved: 47,
    compactedToVersion: 53,
    compactedAt: DateTime.UtcNow
);

// Log the compaction outcome for audit purposes
_logger.Information(
    "Compaction completed: {Result}",
    compactionResult.ToString()
);

// Persist the compaction record for operational metrics
_compactionHistoryRepository.Save(compactionResult);
```

### Example 2: Evaluating Compaction Effectiveness

```csharp
// Retrieve recent compaction results and assess storage savings
var recentCompactions = _compactionHistoryRepository
    .GetSince(DateTime.UtcNow.AddDays(-7));

foreach (var result in recentCompactions)
{
    if (result.EventsRemoved > 100)
    {
        Console.WriteLine(
            $"High-impact compaction on {result.AggregateId}: " +
            $"{result.EventsRemoved} events removed, " +
            $"stream compacted to version {result.CompactedToVersion} " +
            $"at {result.CompactedAt:O}"
        );
    }
}
```

## Notes

- **Immutability:** All properties are read-only and set only through the constructor. Instances are effectively immutable once created, making them safe to share across threads without synchronization.
- **Thread Safety:** The type is thread-safe for concurrent reads. No internal state is mutated after construction, so multiple threads may safely access any property or call `ToString` simultaneously.
- **Timestamp Precision:** The `CompactedAt` property stores a `DateTime` value. Callers should ensure consistency by always supplying UTC timestamps to avoid timezone ambiguity in distributed systems.
- **Version Semantics:** `CompactedToVersion` indicates the version the stream was compacted *to*, meaning events with versions greater than this value remain in the stream. Events at or below this version were candidates for removal based on the compaction strategy.
- **Zero Removals:** `EventsRemoved` may be zero if compaction determined that no events were eligible for removal (e.g., snapshot already up to date, or retention policy prevented deletion). This is a valid result and does not indicate an error.
- **String Representation:** The output of `ToString` is intended for diagnostic and logging purposes. Its exact format is an implementation detail and should not be parsed programmatically.
