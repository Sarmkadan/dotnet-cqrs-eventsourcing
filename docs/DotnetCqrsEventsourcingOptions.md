# DotnetCqrsEventsourcingOptions
The `DotnetCqrsEventsourcingOptions` type provides a set of configuration options for event sourcing in the dotnet-cqrs-eventsourcing project. It allows developers to customize the behavior of the event store, projection store, and snapshot store, as well as control caching, batching, and retention policies. This type is used to fine-tune the performance and reliability of event-sourced systems.

## API
The `DotnetCqrsEventsourcingOptions` type has the following public members:
* `EventStoreConnectionString`: a string representing the connection string to the event store.
* `ProjectionStoreConnectionString`: a string representing the connection string to the projection store.
* `SnapshotStoreConnectionString`: a string representing the connection string to the snapshot store.
* `MaxEventsCached`: an integer specifying the maximum number of events to cache.
* `CacheExpirationSeconds`: an integer specifying the cache expiration time in seconds.
* `EnableEventCompression`: a boolean indicating whether event compression is enabled.
* `BatchWriteSize`: an integer specifying the batch write size.
* `ParallelReaderCount`: an integer specifying the number of parallel readers.
* `AutoCreateSnapshots`: a boolean indicating whether snapshots are automatically created.
* `SnapshotFrequency`: an integer specifying the snapshot frequency.
* `MinVersionForSnapshot`: a long integer specifying the minimum version for snapshotting.
* `VerifyEventChecksums`: a boolean indicating whether event checksums are verified.
* `RetentionPolicy`: an `EventRetentionPolicy` enum value specifying the retention policy.
* `RetentionDays`: an integer specifying the retention period in days.

## Usage
Here are two examples of using the `DotnetCqrsEventsourcingOptions` type:
```csharp
// Example 1: Basic configuration
var options = new DotnetCqrsEventsourcingOptions
{
    EventStoreConnectionString = "eventstore://localhost",
    ProjectionStoreConnectionString = "projectionstore://localhost",
    SnapshotStoreConnectionString = "snapshotstore://localhost",
    MaxEventsCached = 1000,
    CacheExpirationSeconds = 3600
};

// Example 2: Advanced configuration
var advancedOptions = new DotnetCqrsEventsourcingOptions
{
    EventStoreConnectionString = "eventstore://localhost",
    ProjectionStoreConnectionString = "projectionstore://localhost",
    SnapshotStoreConnectionString = "snapshotstore://localhost",
    EnableEventCompression = true,
    BatchWriteSize = 100,
    ParallelReaderCount = 4,
    AutoCreateSnapshots = true,
    SnapshotFrequency = 10,
    MinVersionForSnapshot = 5,
    VerifyEventChecksums = true,
    RetentionPolicy = EventRetentionPolicy.Delete,
    RetentionDays = 30
};
```

## Notes
When using the `DotnetCqrsEventsourcingOptions` type, consider the following edge cases and thread-safety remarks:
* The `EventStoreConnectionString`, `ProjectionStoreConnectionString`, and `SnapshotStoreConnectionString` properties should be set to valid connection strings to ensure proper functionality.
* The `MaxEventsCached` and `CacheExpirationSeconds` properties control caching behavior, which can impact performance. Adjust these values according to the specific use case.
* The `EnableEventCompression` property can improve storage efficiency but may increase processing time.
* The `BatchWriteSize` and `ParallelReaderCount` properties can improve write and read performance, respectively, but may also increase resource utilization.
* The `AutoCreateSnapshots` and `SnapshotFrequency` properties control snapshot creation, which can impact storage requirements and query performance.
* The `MinVersionForSnapshot` property ensures that snapshots are only created for events with a version greater than or equal to the specified value.
* The `VerifyEventChecksums` property ensures data integrity by verifying event checksums.
* The `RetentionPolicy` and `RetentionDays` properties control the retention period for events, which can impact storage requirements and data availability.
* The `DotnetCqrsEventsourcingOptions` type is not thread-safe by default. If used in a multi-threaded environment, consider using synchronization mechanisms or creating a new instance for each thread.
