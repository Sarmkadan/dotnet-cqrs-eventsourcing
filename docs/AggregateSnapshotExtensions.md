# AggregateSnapshotExtensions

AggregateSnapshotExtensions provides a set of static extension methods for working with `AggregateSnapshot` instances. The methods enable common operations such as creating deep copies, updating payload data, comparing snapshot versions, and applying custom compression to the snapshot’s data without modifying the original instance.

## API

### DeepCopy
```csharp
public static AggregateSnapshot DeepCopy(AggregateSnapshot source)
```
**Purpose:** Produces a deep copy of the supplied snapshot, ensuring that the returned object does not share mutable references with the original.  
**Parameters:**  
- `source`: The snapshot to copy.  
**Return Value:** A new `AggregateSnapshot` instance with the same property values as `source` but with an independent object graph.  
**Exceptions:**  
- `ArgumentNullException` if `source` is `null`.

### WithUpdatedData
```csharp
public static AggregateSnapshot WithUpdatedData(this AggregateSnapshot snapshot, object newData)
```
**Purpose:** Returns a new snapshot whose data payload is replaced with `newData`, preserving all other properties (such as version and timestamp).  
**Parameters:**  
- `snapshot`: The snapshot to update.  
- `newData`: The object that will become the snapshot’s data.  
**Return Value:** A new `AggregateSnapshot` instance containing `newData` and the unchanged metadata from `snapshot`.  
**Exceptions:**  
- `ArgumentNullException` if `snapshot` is `null`.

### IsNewerThan
```csharp
public static bool IsNewerThan(AggregateSnapshot left, AggregateSnapshot right)
```
**Purpose:** Determines whether the `left` snapshot represents a newer state than the `right` snapshot by comparing their version numbers (or timestamps, depending on the implementation).  
**Parameters:**  
- `left`: The snapshot to evaluate as potentially newer.  
- `right`: The snapshot to evaluate as potentially newer.  
- `right`: The snapshot to compare against.  
**Return Value:** `true` if `left` is newer than `true` if `left` is newer than `right`; otherwise `false`.**  
**Exceptions:**  
- `ArgumentNullException` if either `left` or `right` is `null`.

### WithCompressedData
```csharp
public static AggregateSnapshot WithCompressedData(this AggregateSnapshot snapshot, Func<string, /* omitted */> compressor)
```
**Purpose:** Returns a new snapshot where the data field has been transformed by the supplied `compressor` function, leaving other metadata unchanged.  
**Parameters:**  
- `snapshot`: The snapshot whose data will be compressed.  
- `compressor`: A function that accepts the current data (as a string) and returns a compressed representation (the exact return type is omitted in the source signature).  
**Return Value:** A new `AggregateSnapshot` instance with the data replaced by the result of `compressor`, while version, timestamp, and other fields remain identical to those of `snapshot`.  
**Exceptions:**  
- `ArgumentNullException` if `snapshot` or `compressor` is `null`.  
- Any exception thrown by `compressor` is propagated to the caller.

## Usage

### Example 1: Copying and updating a snapshot
```csharp
var original = new AggregateSnapshot { Version = 1, Data = @"{""Name"":""Alice""}" };

// Create a deep copy for safe manipulation
var copy = AggregateSnapshotExtensions.DeepCopy(original);

// Update the copy with new data
var updated = copy.WithUpdatedData(new { Name = "Alice", Age = 30 });

// original remains unchanged; updated contains the new payload
```

### Example 2: Version comparison and compression
```csharp
var snapV1 = new AggregateSnapshot { Version = 1, Data = @"{""value"":42}" };
var snapV2 = new AggregateSnapshot { Version = 2, Data = @"{""value"":43}" };

// Determine which snapshot is newer
bool isV2Newer = AggregateSnapshotExtensions.IsNewerThan(snapV2, snapV1); // true

// Compress the data using a custom compressor (e.g., Base64 encoding as a stand‑in)
AggregateSnapshot compressed = snapV2.WithCompressedData(data =>
{
    var bytes = System.Text.Encoding.UTF8.GetBytes(data);
    return Convert.ToBase64String(bytes); // return type matches the omitted generic
});

// compressed.Data now holds the Base64‑encoded string; Version is still 2
```

## Notes
- All methods are pure; they do not modify the input snapshot instance and rely only on their arguments. Consequently, they are thread‑safe to invoke concurrently from multiple threads, provided that any delegate supplied to `WithCompressedData` is itself thread‑safe or does not depend on mutable shared state.  
- Passing `null` for any snapshot argument results in an `ArgumentNullException`.  
- The `WithCompressedData` method does not enforce any particular compression algorithm; correctness and performance depend entirely on the user‑provided `compressor` delegate. Callers should ensure the delegate does not retain references to the input string in a way that could cause unintended side effects.  
- Version comparison in `IsNewerThan` assumes that higher version numbers indicate a newer snapshot; if the implementation uses timestamps instead, the same ordering principle applies.  
- Because the methods return new instances, callers are responsible for managing the lifecycle of the returned snapshots (e.g., disposal if the type implements `IDisposable`). The extensions themselves do not allocate any long‑lived resources beyond the returned objects.
