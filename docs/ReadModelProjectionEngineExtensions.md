# ReadModelProjectionEngineExtensions

The `ReadModelProjectionEngineExtensions` class provides a set of static extension methods designed to interrogate and monitor the runtime state of a read model projection engine within the CQRS event sourcing architecture. These utilities enable developers to retrieve checkpoint data, verify synchronization status across projections, aggregate processing metrics, and detect operational errors without requiring direct access to the internal state of the projection engine itself.

## API

### `GetOrCreateCheckpoint`
Retrieves the current checkpoint for a specific projection or initializes a new one if it does not exist.
*   **Purpose**: Ensures a valid checkpoint object is available for a named projection, facilitating state recovery or initialization.
*   **Parameters**: Accepts the target projection engine instance and the name of the projection.
*   **Return Value**: Returns a `ProjectionCheckpoint` object representing the current or newly created state.
*   **Exceptions**: May throw an exception if the projection name is null, empty, or if the underlying storage mechanism fails during creation.

### `AllProjectionsAtVersionOrHigher`
Validates whether every registered projection has processed events up to at least a specified global version.
*   **Purpose**: Used to determine system readiness or consistency before executing queries that depend on fully synchronized read models.
*   **Parameters**: Accepts the projection engine instance and a `long` value representing the required minimum event version.
*   **Return Value**: Returns `true` if all projections meet the version requirement; otherwise, `false`.
*   **Exceptions**: Generally does not throw unless the engine is in an invalid state.

### `GetProjectionNamesWithCheckpoints`
Enumerates the names of all projections that currently have an associated checkpoint stored in the system.
*   **Purpose**: Provides visibility into which projections are actively tracking state versus those that may be unconfigured or transient.
*   **Parameters**: Accepts the projection engine instance.
*   **Return Value**: Returns an `IEnumerable<string>` containing the names of projections with existing checkpoints.
*   **Exceptions**: Does not typically throw; returns an empty enumerable if no checkpoints exist.

### `GetTotalEventsProcessed`
Calculates the aggregate count of events successfully processed across all managed projections.
*   **Purpose**: Offers a high-level metric for system throughput and overall processing volume.
*   **Parameters**: Accepts the projection engine instance.
*   **Return Value**: Returns a `long` representing the total number of events processed.
*   **Exceptions**: Does not typically throw.

### `HasAnyProjectionErrors`
Checks the health status of the projection engine to determine if any active projection has encountered a processing error.
*   **Purpose**: Essential for monitoring dashboards and alerting systems to detect stalled or failed projections.
*   **Parameters**: Accepts the projection engine instance.
*   **Return Value**: Returns `true` if at least one projection is in an error state; otherwise, `false`.
*   **Exceptions**: Does not typically throw.

### `GetLastProcessedEventId`
Retrieves the identifier of the most recent event successfully processed by the engine.
*   **Purpose**: Useful for auditing, debugging, and verifying the continuity of the event stream consumption.
*   **Parameters**: Accepts the projection engine instance.
*   **Return Value**: Returns a `string?` containing the event ID, or `null` if no events have been processed yet.
*   **Exceptions**: Does not typically throw.

### `GetLastUpdatedTimestamp`
Returns the date and time when the projection engine last successfully processed an event or updated its state.
*   **Purpose**: Helps identify stale projections that may have stopped consuming events despite no explicit error flags.
*   **Parameters**: Accepts the projection engine instance.
*   **Return Value**: Returns a `DateTime` indicating the last activity timestamp.
*   **Exceptions**: Does not typically throw.

### `IsProjectionActive`
Determines whether a specific named projection is currently running and actively consuming events.
*   **Purpose**: Distinguishes between projections that are configured but paused, stopped, or never started.
*   **Parameters**: Accepts the projection engine instance and the name of the projection.
*   **Return Value**: Returns `true` if the projection is active; otherwise, `false`.
*   **Exceptions**: May throw if the provided projection name does not correspond to any known projection.

### `GetProjectionStatuses` (Inferred from `IReadOnlyDictionary<string, ...>`)
Retrieves a comprehensive snapshot of the status for all known projections.
*   **Purpose**: Provides a bulk view of the system state for administrative tools or detailed logging.
*   **Parameters**: Accepts the projection engine instance.
*   **Return Value**: Returns an `IReadOnlyDictionary<string, TStatus>` where the key is the projection name and the value represents its specific status object.
*   **Exceptions**: Does not typically throw.

## Usage

### Checking System Readiness Before Querying
The following example demonstrates how to verify that all projections have caught up to the latest write model version before allowing a critical read operation to proceed.

```csharp
public async Task<bool> IsSystemReadyForQueryAsync(
    IReadModelProjectionEngine engine, 
    long currentWriteModelVersion)
{
    // Check if any projections are in an error state first
    if (engine.HasAnyProjectionErrors())
    {
        return false;
    }

    // Verify all projections have processed events up to the required version
    bool isSynchronized = engine.AllProjectionsAtVersionOrHigher(currentWriteModelVersion);
    
    return isSynchronized;
}
```

### Monitoring Projection Health and Activity
This example illustrates how to gather diagnostic information about specific projections to generate a health report.

```csharp
public void GenerateProjectionHealthReport(IReadModelProjectionEngine engine)
{
    var projectionNames = engine.GetProjectionNamesWithCheckpoints();
    
    foreach (var name in projectionNames)
    {
        bool isActive = engine.IsProjectionActive(name);
        var checkpoint = engine.GetOrCreateCheckpoint(name);
        
        Console.WriteLine($"Projection: {name}");
        Console.WriteLine($"  Active: {isActive}");
        Console.WriteLine($"  Current Version: {checkpoint.Version}");
        Console.WriteLine($"  Last Updated: {engine.GetLastUpdatedTimestamp()}");
        
        if (!isActive)
        {
            Console.WriteLine("  WARNING: Projection is inactive.");
        }
    }

    Console.WriteLine($"Total Events Processed: {engine.GetTotalEventsProcessed()}");
    Console.WriteLine($"Last Event ID: {engine.GetLastProcessedEventId() ?? "None"}");
}
```

## Notes

*   **Thread Safety**: As these methods operate on the live state of the projection engine, they are generally thread-safe for read operations. However, the state returned (such as a `ProjectionCheckpoint`) represents a snapshot in time. In highly concurrent environments, the status of a projection (e.g., `IsProjectionActive`) may change immediately after the method returns.
*   **Side Effects**: While most methods are purely observational, `GetOrCreateCheckpoint` may induce a side effect by initializing a new checkpoint record in the underlying store if one does not exist. This should be considered when calling this method in read-only diagnostic loops.
*   **Null Handling**: Methods returning nullable types (e.g., `GetLastProcessedEventId`) will return `null` if the engine has not yet processed any events. Callers must handle these null cases explicitly to avoid `NullReferenceException`.
*   **Performance**: Methods returning collections (e.g., `GetProjectionNamesWithCheckpoints`, the dictionary status method) enumerate the current state of all projections. On systems with a very large number of projections, frequent invocation of these methods may incur minor overhead due to snapshot generation.
*   **Error States**: The `HasAnyProjectionErrors` flag indicates that an error *has occurred* and may persist until the projection is restarted or the error is resolved. It does not necessarily imply that the engine is completely halted, as other projections may continue to function normally.
