# EventEnvelope

The `EventEnvelope` class serves as the standardized container for events within the `dotnet-cqrs-eventsourcing` infrastructure, encapsulating both the payload and the contextual metadata required for event sourcing and CQRS patterns. It binds a specific event data string to its corresponding aggregate identity, versioning information, and type descriptors, while providing built-in mechanisms for data integrity verification via checksums and optional partitioning strategies for storage scalability.

## API

### Properties

#### `public string Id`
Gets the unique identifier for this specific event envelope instance. This ID distinguishes the event record within the event store, independent of the aggregate it belongs to.

#### `public string AggregateId`
Gets the identifier of the aggregate root to which this event belongs. This property is essential for reconstructing aggregate state by filtering events belonging to a specific entity.

#### `public string AggregateType`
Gets the fully qualified name or designated type identifier of the aggregate root. This ensures that events are applied only to aggregates of the correct type during hydration.

#### `public long AggregateVersion`
Gets the version number of the aggregate immediately after this event was applied. This value is monotonic within a specific `AggregateId` stream and is used for optimistic concurrency control.

#### `public string EventType`
Gets the type identifier of the event payload contained within `EventData`. This allows the deserializer to instantiate the correct concrete event class from the serialized data.

#### `public string EventData`
Gets the serialized representation of the event payload. The format (e.g., JSON, Protobuf) is determined by the serializer configured in the event store, but is stored here as a string.

#### `public Dictionary<string, string> Metadata`
Gets the collection of key-value pairs containing contextual information about the event, such as correlation IDs, causation IDs, user identifiers, or timestamps originating from the client.

#### `public DateTime CreatedAt`
Gets the UTC timestamp indicating when this event envelope was created and persisted to the store.

#### `public string? ChecksumHash`
Gets the computed hash value used to verify the integrity of the event data and critical headers. This property is `null` if a checksum has not yet been computed or if integrity checking is disabled.

#### `public string? PartitionKey`
Gets the optional key used to determine the physical partition or shard where this event should be stored. If `null`, the default partitioning strategy (usually based on `AggregateId`) is applied.

### Constructors

#### `public EventEnvelope()`
Initializes a new instance of the `EventEnvelope` class with default values. Properties must be set manually after instantiation.

#### `public EventEnvelope(string aggregateId, string aggregateType, long aggregateVersion, string eventType, string eventData)`
Initializes a new instance of the `EventEnvelope` class with the core event sourcing parameters.
*   **Parameters**:
    *   `aggregateId`: The ID of the target aggregate.
    *   `aggregateType`: The type name of the target aggregate.
    *   `aggregateVersion`: The resulting version of the aggregate.
    *   `eventType`: The type name of the event payload.
    *   `eventData`: The serialized event payload.
*   **Behavior**: Initializes `Id` with a new unique identifier, sets `CreatedAt` to the current UTC time, and initializes an empty `Metadata` dictionary. `ChecksumHash` and `PartitionKey` are initialized to `null`.

### Methods

#### `public void ComputeChecksum()`
Calculates a cryptographic hash based on the `AggregateId`, `AggregateVersion`, `EventType`, and `EventData` properties and assigns the result to the `ChecksumHash` property.
*   **Return Value**: None.
*   **Side Effects**: Updates the `ChecksumHash` property.
*   **Exceptions**: Throws an exception if any of the required fields used for hashing are null or in an invalid state.

#### `public bool VerifyChecksum()`
Validates the integrity of the envelope by recomputing the checksum and comparing it against the existing `ChecksumHash` value.
*   **Return Value**: Returns `true` if the computed hash matches the stored `ChecksumHash`; otherwise, returns `false`. If `ChecksumHash` is `null`, returns `false`.
*   **Exceptions**: None.

#### `public override string ToString()`
Returns a string representation of the `EventEnvelope`, typically including the `Id`, `AggregateId`, `AggregateVersion`, and `EventType` for logging and debugging purposes.
*   **Return Value**: A formatted string describing the event envelope.
*   **Exceptions**: None.

## Usage

### Creating and Persisting an Event Envelope

The following example demonstrates instantiating an envelope with serialized event data, adding correlation metadata, and computing the integrity checksum before persistence.

```csharp
using System;
using System.Collections.Generic;

// Assume eventData is a JSON string generated by a serializer
string eventData = "{\"OrderId\":\"ord_123\",\"Status\":\"Confirmed\"}";

var envelope = new EventEnvelope(
    aggregateId: "ord_123",
    aggregateType: "OrderAggregate",
    aggregateVersion: 5,
    eventType: "OrderConfirmed",
    eventData: eventData
);

// Add contextual metadata
envelope.Metadata["CorrelationId"] = "corr_987";
envelope.Metadata["UserId"] = "user_456";
envelope.Metadata["OccurredAt"] = DateTime.UtcNow.ToString("O");

// Set a custom partition key if required by the infrastructure
envelope.PartitionKey = "region_us_east";

// Compute integrity hash before saving
envelope.ComputeChecksum();

// The envelope is now ready to be passed to the EventStore
Console.WriteLine(envelope.ToString());
```

### Verifying Event Integrity During Hydration

When reading events from the store to reconstruct an aggregate, the checksum should be verified to ensure the data has not been corrupted or tampered with.

```csharp
public void HydrateAggregate(EventEnvelope[] events)
{
    foreach (var evt in events)
    {
        // Verify data integrity
        if (!evt.VerifyChecksum())
        {
            throw new InvalidOperationException(
                $"Checksum verification failed for event {evt.Id} in aggregate {evt.AggregateId}. " +
                "Data corruption detected."
            );
        }

        // Deserialize and apply event logic here
        // var domainEvent = serializer.Deserialize(evt.EventData, evt.EventType);
        // aggregate.Apply(domainEvent);
    }
}
```

## Notes

*   **Immutability Considerations**: While the class properties have public setters (implied by the constructor usage and typical POCO patterns in this domain), modifying properties such as `AggregateVersion`, `EventData`, or `AggregateId` after calling `ComputeChecksum()` will invalidate the `ChecksumHash`. `VerifyChecksum()` must be called again after any mutation to ensure validity.
*   **Thread Safety**: The `EventEnvelope` class is not thread-safe. Concurrent modifications to the `Metadata` dictionary or the mutable properties (`EventData`, `ChecksumHash`, etc.) from multiple threads without external synchronization may result in race conditions or inconsistent state.
*   **Null Handling**: The `ChecksumHash` and `PartitionKey` properties are nullable. Consumers must handle `null` values appropriately; specifically, `VerifyChecksum()` returns `false` if `ChecksumHash` is `null`, rather than throwing an exception.
*   **Serialization**: The `EventData` property expects a pre-serialized string. The `EventEnvelope` itself does not perform serialization of the domain event object; this must be handled by the caller before assigning the `EventData` property.
*   **Versioning**: The `AggregateVersion` is expected to be strictly sequential per `AggregateId`. Gaps or duplicates in version numbers within a stream usually indicate a logic error in the command handling or event appending process.
