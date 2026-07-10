# LoggingDecorator
The `LoggingDecorator` type is designed to provide logging functionality for various operations within the CQRS event sourcing framework, allowing for the tracking and monitoring of system activities, such as event publishing, processing, and errors, as well as aggregate operations, concurrency conflicts, snapshot creation, and projection rebuilding. It also inherits from `PerformanceDecorator`, enabling performance tracking and summary generation.

## API
* `public LoggingDecorator`: The constructor for the `LoggingDecorator` class, used to create a new instance.
* `public void LogEventPublished`: Logs an event that has been published. This method does not return a value and does not throw any exceptions based on its signature.
* `public void LogEventProcessed`: Logs an event that has been processed. This method does not return a value and does not throw any exceptions based on its signature.
* `public void LogEventProcessingError`: Logs an error that occurred during event processing. This method does not return a value and does not throw any exceptions based on its signature.
* `public void LogAggregateOperation`: Logs an operation performed on an aggregate. This method does not return a value and does not throw any exceptions based on its signature.
* `public void LogConcurrencyConflict`: Logs a concurrency conflict that has occurred. This method does not return a value and does not throw any exceptions based on its signature.
* `public void LogSnapshotCreated`: Logs the creation of a snapshot. This method does not return a value and does not throw any exceptions based on its signature.
* `public void LogProjectionRebuilt`: Logs the rebuilding of a projection. This method does not return a value and does not throw any exceptions based on its signature.
* `public PerformanceDecorator`: The base class for performance tracking, inherited by `LoggingDecorator`.
* `public void TrackOperation`: Tracks a specific operation for performance monitoring. This method does not return a value and does not throw any exceptions based on its signature.
* `public string GetPerformanceSummary`: Returns a summary of the tracked performance data. This method returns a string and does not throw any exceptions based on its signature.

## Usage
The following examples demonstrate how to use the `LoggingDecorator` to log events and track performance:
```csharp
// Example 1: Logging event publication and processing
var logger = new LoggingDecorator();
logger.LogEventPublished("EventPublished");
logger.LogEventProcessed("EventProcessed");

// Example 2: Tracking performance and logging aggregate operation
var performanceLogger = new LoggingDecorator();
performanceLogger.TrackOperation("AggregateOperation");
performanceLogger.LogAggregateOperation("AggregateOperationPerformed");
var performanceSummary = performanceLogger.GetPerformanceSummary();
```

## Notes
When using the `LoggingDecorator`, consider the following:
- Since the logging methods do not throw exceptions based on their signatures, error handling should be implemented according to the specific logging implementation.
- The `LoggingDecorator` inherits from `PerformanceDecorator`, which means it also provides performance tracking capabilities.
- Thread-safety should be considered when using the `LoggingDecorator` in multi-threaded environments, as the logging and performance tracking operations may not be inherently thread-safe.
- Edge cases, such as logging null or empty events, should be handled according to the specific requirements of the application.
