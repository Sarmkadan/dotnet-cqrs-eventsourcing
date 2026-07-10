# IncrementalSnapshot

Represents an incremental snapshot in an event‑sourced aggregate, storing the delta changes from a base snapshot together with metadata needed for integrity verification and chain management.

## API

### Id  
**string Id**  
Gets or sets the unique identifier of this incremental snapshot.  
*Throws*: `ArgumentException` if set to `null`, empty, or whitespace.

### AggregateId  
**string AggregateId**  
Gets or sets the identifier of the aggregate to which this snapshot belongs.  
*Throws*: `ArgumentException` if set to `null`, empty, or whitespace.

### AggregateType  
**string AggregateType**  
Gets or sets the assembly‑qualified name of the aggregate type.  
*Throws*: `ArgumentException` if set to `null`, empty, or whitespace.

### Version  
**long Version**  
Gets or sets the aggregate version after applying this snapshot’s delta.  
*Throws*: `ArgumentOutOfRangeException` if set to a negative value.

### BaseVersion  
**long BaseVersion**  
Gets or sets the version of the base snapshot that this delta is based on.  
*Throws*: `ArgumentOutOfRangeException` if set to a negative value or greater than or equal to `Version`.

### BaseSnapshotId  
**string BaseSnapshotId**  
Gets or sets the identifier of the base snapshot.  
*Throws*: `ArgumentException` if set to `null`, empty, or whitespace.

### DeltaData  
**string DeltaData**  
Gets or sets the serialized delta (optionally compressed) representing the changes from the base snapshot to this version.  
*Throws*: `ArgumentNullException` if set to `null`.

### SequenceNumber  
**int SequenceNumber**  
Gets or sets the sequential number of this incremental snapshot within its chain, starting at 1 for the first delta after the base snapshot.  
*Throws*: `ArgumentOutOfRangeException` if set to a value less than 1.

### IsCompressed  
**bool IsCompressed**  
Gets or sets a flag indicating whether `DeltaData` is stored in a compressed form.  
No exceptions are thrown by the property itself.

### CreatedAt  
**DateTime CreatedAt**  
Gets or sets the UTC timestamp when the snapshot was persisted.  
*Throws*: `ArgumentOutOfRangeException` if set to a value earlier than `DateTime.MinValue` or later than `DateTime.MaxValue`.

### Checksum  
**string? Checksum**  
Gets or sets an optional checksum used to verify the integrity of `DeltaData`. May be `null` if no checksum has been computed.  
No exceptions are thrown by the property itself.

### IncrementalSnapshot (constructor)  
**public IncrementalSnapshot(...)**  
Initializes a new instance of the `IncrementalSnapshot` class. The constructor’s parameters correspond to the writable properties of the type (Id, AggregateId, AggregateType, Version, BaseVersion, BaseSnapshotId, DeltaData, SequenceNumber, IsCompressed, CreatedAt, Checksum).  
*Throws*:  
- `ArgumentNullException` for any required string parameter that is `null`.  
- `ArgumentException` for any string parameter that is empty or whitespace.  
- `ArgumentOutOfRangeException` for any numeric parameter outside its valid range.

### Create  
**public static IncrementalSnapshot Create(string id, string aggregateId, string aggregateType, long version, long baseVersion, string baseSnapshotId, string deltaData, int sequenceNumber, bool isCompressed, DateTime createdAt, string? checksum = null)**  
Factory method that creates and returns a new `IncrementalSnapshot` instance after validating the supplied arguments.  
*Parameters*:  
- `id` – unique identifier for the snapshot.  
- `aggregateId` – identifier of the owning aggregate.  
- `aggregateType` – CLR type name of the aggregate.  
- `version` – aggregate version after applying the delta.  
- `baseVersion` – version of the base snapshot.  
- `baseSnapshotId` – identifier of the base snapshot.  
- `deltaData` – serialized (optionally compressed) delta.  
- `sequenceNumber` – position in the incremental chain.  
- `isCompressed` – indicates whether `deltaData` is compressed.  
- `createdAt` – UTC timestamp of creation.  
- `checksum` – optional pre‑computed checksum.  
*Returns*: A fully initialized `IncrementalSnapshot`.  
*Throws*: Same exceptions as the constructor for invalid arguments.

### ComputeChecksum  
**public void ComputeChecksum()**  
Calculates a checksum (e.g., SHA‑256) over the `DeltaData` bytes, respecting the `IsCompressed` flag, and stores the result in the `Checksum` property.  
*Throws*:  
- `InvalidOperationException` if `DeltaData` is `null`.  
- Any exception thrown by the underlying hashing algorithm (e.g., `ArgumentNullException`).

### VerifyChecksum  
**public bool VerifyChecksum()**  
Recomputes the checksum for `DeltaData` and compares it to the stored `Checksum` value. Returns `true` if they match or if `Checksum` is `null` (treated as unverified).  
*Throws*:  
- `InvalidOperationException` if `DeltaData` is `null`.  
- Any exception thrown by the hashing algorithm.

### ToString  
**public override string ToString()**  
Returns a string that displays the snapshot’s key identifiers: `Id`, `AggregateId`, `Version`, and `SequenceNumber`. Useful for logging and debugging.  
*Throws*: None.

### BaseSnapshot  
**public AggregateSnapshot BaseSnapshot { get; set; }**  
Gets or sets the base snapshot to which this incremental snapshot is attached.  
*Throws*: `ArgumentNullException` if set to `null`.

### Incrementals  
**public IReadOnlyList<IncrementalSnapshot> Incrementals { get; }**  
Gets a read‑only list of incremental snapshots that follow this one in the chain (i.e., those with a higher `SequenceNumber`). The list is empty if there are no subsequent deltas.  
*Throws*: None.

### IncrementalSnapshotChain  
**public IncrementalSnapshotChain IncrementalSnapshotChain { get; set; }**  
Gets or sets the chain object that groups this snapshot with its base and subsequent incrementals, enabling chain‑wide operations such as collapse or validation.  
*Throws*: `ArgumentNullException` if set to `null`.

### ShouldCollapse  
**public bool ShouldCollapse { get; }**  
Gets a value indicating whether this snapshot should be collapsed into a new base snapshot (e.g., when the number of incrementals exceeds a threshold or the delta size grows too large). The logic is internal to the type and depends on `Incrementals.Count`, `IsCompressed`, and other heuristics.  
*Throws*: None.

## Usage

### Example 1: Creating and verifying an incremental snapshot
```csharp
using DotNetCqrsEventSourcing;

// Assume we have an existing base snapshot
var baseSnapshot = await snapshotStore.GetBaseSnapshotAsync(aggregateId);

// Prepare delta data (could be compressed)
string delta = Compress(serializedEvents);
bool isCompressed = true;

// Create the incremental snapshot
var inc = IncrementalSnapshot.Create(
    id: Guid.NewGuid().ToString(),
    aggregateId: aggregateId,
    aggregateType: typeof(MyAggregate).AssemblyQualifiedName!,
    version: baseSnapshot.Version + 5,
    baseVersion: baseSnapshot.Version,
    baseSnapshotId: baseSnapshot.Id,
    deltaData: delta,
    sequenceNumber: 1,
    isCompressed: isCompressed,
    createdAt: DateTime.UtcNow);

// Compute and attach a checksum
inc.ComputeChecksum();

// Persist the snapshot
await snapshotStore.SaveIncrementalAsync(inc);

// Later, when reading it back, verify integrity
var loaded = await snapshotStore.GetIncrementalAsync(inc.Id);
bool isValid = loaded.VerifyChecksum(); // true if not tampered
```

### Example 2: Traversing a snapshot chain and deciding on collapse
```csharp
using DotNetCqrsEventSourcing;

// Load the base snapshot for an aggregate
var baseSnap = await snapshotStore.GetBaseSnapshotAsync(aggregateId);

// Load all incrementals linked to this base
var chain = await snapshotStore.LoadIncrementalChainAsync(baseSnap.Id);

// Walk through the chain from the base upwards
IncrementalSnapshot? current = null;
foreach (var inc in chain.Incrementals)
{
    // Example: apply delta to rebuild state (pseudo‑code)
    // state = ApplyDelta(state, inc.DeltaData, inc.IsCompressed);

    current = inc; // keep reference to the latest incremental
}

// After processing, check if the chain should be collapsed
if (current != null && current.ShouldCollapse)
{
    // Trigger a collapse operation that creates a new base snapshot
    var newBase = await snapshotStore.CollapseChainAsync(chain);
    await snapshotStore.SaveBaseSnapshotAsync(newBase);
}
```

## Notes

- The `Checksum` property is optional; setting it to `null` disables automatic verification. When a checksum is present, both `ComputeChecksum` and `VerifyChecksum` operate on the raw bytes of `DeltaData`, decompressing it first if `IsCompressed` is `true`.
- All string‑based identifiers (`Id`, `AggregateId`, `AggregateType`, `BaseSnapshotId`, `Checksum`) are validated for `null` or empty values upon assignment; attempting to set an invalid value throws an `ArgumentException`.
- Numeric properties (`Version`, `BaseVersion`, `SequenceNumber`) are guarded against negative values; `BaseVersion` must be strictly less than `Version`.
- The type itself does not enforce immutability after construction; however, typical usage treats instances as immutable once persisted. Concurrent modification of the same instance from multiple threads is not thread‑safe; external synchronization is required if shared mutable state is needed.
- The `Incrementals` collection is a snapshot of the chain at the time it is retrieved; modifications to the underlying store after retrieval are not reflected in the existing list.
- `ShouldCollapse` is evaluated based on internal heuristics that may change between library versions; consumers should treat it as a advisory flag and not rely on a specific threshold value.
