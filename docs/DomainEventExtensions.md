# DomainEventExtensions

Provides static utility methods for serializing, cloning, and enriching `DomainEvent` instances. These helpers are intended to standardize JSON representation and metadata handling across event-sourced aggregates without introducing external dependencies or altering the core event contract.

## API

### `public static string ToJson(this DomainEvent domainEvent)`

Serializes a `DomainEvent` to its compact JSON representation.

- **Parameters:**  
  `domainEvent` — the event to serialize. Must not be `null`.

- **Return value:**  
  A non-indented JSON string that represents the event’s data and metadata.

- **Exceptions:**  
  Throws `ArgumentNullException` when `domainEvent` is `null`.  
  Throws `JsonSerializationException` (or the serializer-specific equivalent) when the event graph cannot be serialized.

### `public static string ToJsonPretty(this DomainEvent domainEvent)`

Serializes a `DomainEvent` to an indented, human-readable JSON string.

- **Parameters:**  
  `domainEvent` — the event to serialize. Must not be `null`.

- **Return value:**  
  A multi-line, indented JSON string.

- **Exceptions:**  
  Throws `ArgumentNullException` when `domainEvent` is `null`.  
  Throws `JsonSerializationException` when the event graph cannot be serialized.

### `public static DomainEvent WithMetadata(this DomainEvent domainEvent, string key, object value)`

Creates a new `DomainEvent` that carries an additional metadata entry while preserving the original event’s identity and payload.

- **Parameters:**  
  `domainEvent` — the source event. Must not be `null`.  
  `key` — the metadata key to add or overwrite. Must not be `null` or empty.  
  `value` — the metadata value to associate with the key. Can be `null`.

- **Return value:**  
  A new `DomainEvent` instance whose metadata dictionary contains the specified entry. The original event is not modified.

- **Exceptions:**  
  Throws `ArgumentNullException` when `domainEvent` or `key` is `null`.  
  Throws `ArgumentException` when `key` is empty or consists only of white space.

### `public static DomainEvent Clone(this DomainEvent domainEvent)`

Produces a deep copy of the given `DomainEvent`, including its payload and metadata.

- **Parameters:**  
  `domainEvent` — the event to clone. Must not be `null`.

- **Return value:**  
  A new `DomainEvent` instance that is structurally equal to the original but shares no mutable references.

- **Exceptions:**  
  Throws `ArgumentNullException` when `domainEvent` is `null`.  
  Throws `JsonSerializationException` (or equivalent) when the event cannot be round-tripped through the internal serialization mechanism used for cloning.

## Usage

### Example 1: Enriching an event with correlation metadata before publishing

```csharp
DomainEvent original = new OrderPlaced(orderId, customerId, total);

DomainEvent enriched = original
    .WithMetadata("CorrelationId", Guid.NewGuid().ToString())
    .WithMetadata("CausationId", command.MessageId);

string payload = enriched.ToJson();
eventBus.Publish(payload);
```

### Example 2: Cloning an event for a read-model projection while retaining a raw copy

```csharp
DomainEvent committed = eventStore.Load(streamId, version);

// Keep an untouched copy for auditing
DomainEvent auditCopy = committed.Clone();

// Attach projection-specific metadata without polluting the audit copy
DomainEvent projectionEvent = committed.WithMetadata("ProjectionAttempt", DateTime.UtcNow);

await projectionEngine.Handle(projectionEvent);
auditLog.Write(auditCopy.ToJsonPretty());
```

## Notes

- **Immutability:** `WithMetadata` and `Clone` always return new instances. The source `DomainEvent` remains unchanged, making these methods safe to use in multi-threaded projections or pipelines without locks.
- **Deep cloning:** `Clone` typically relies on serialize-deserialize round-tripping. Events containing non-serializable fields (e.g., live database connections, open streams) will cause serialization failures. Keep event payloads serialization-friendly.
- **Metadata overwrites:** `WithMetadata` overwrites an existing key silently. If preserving previous values is required, retrieve the existing metadata dictionary first and merge manually before calling the method.
- **Thread safety:** All methods are static and operate on immutable inputs or produce new outputs. They do not mutate shared state and are safe to invoke concurrently.
- **Null metadata values:** `WithMetadata` accepts `null` as a metadata value. The resulting JSON representation will include the key with a JSON `null` token. Consumers that strip null-valued keys should perform post-processing if needed.
- **Serialization format:** `ToJson` and `ToJsonPretty` use the same underlying serializer configuration. Differences in output are limited to indentation; the semantic content is identical.
