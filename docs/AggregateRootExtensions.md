# AggregateRootExtensions

Provides extension methods for `AggregateRoot` types to inspect and manage uncommitted event state, including checking for pending changes and retrieving event age.

## API

### `HasUncommittedEvents(AggregateRoot aggregateRoot)`
Determines whether the specified aggregate root has any uncommitted events.

- **Parameters**
  - `aggregateRoot` – The aggregate root instance to inspect.
- **Return Value**
  Returns `true` if the aggregate root has one or more uncommitted events; otherwise, `false`.
- **Exceptions**
  Throws `ArgumentNullException` if `aggregateRoot` is `null`.

---

### `UncommittedEventsCount(AggregateRoot aggregateRoot)`
Gets the number of uncommitted events currently held by the aggregate root.

- **Parameters**
  - `aggregateRoot` – The aggregate root instance to inspect.
- **Return Value**
  Returns the count of uncommitted events as an `int`.
- **Exceptions**
  Throws `ArgumentNullException` if `aggregateRoot` is `null`.

---
### `IsModified(AggregateRoot aggregateRoot)`
Indicates whether the aggregate root has been modified since the last commit.

- **Parameters**
  - `aggregateRoot` – The aggregate root instance to inspect.
- **Return Value**
  Returns `true` if the aggregate root has pending changes (uncommitted events); otherwise, `false`.
- **Exceptions**
  Throws `ArgumentNullException` if `aggregateRoot` is `null`.

---
### `GetAge(AggregateRoot aggregateRoot)`
Calculates the time elapsed since the oldest uncommitted event was recorded.

- **Parameters**
  - `aggregateRoot` – The aggregate root instance to inspect.
- **Return Value**
  Returns a `TimeSpan` representing the duration since the oldest uncommitted event.
- **Exceptions**
  Throws `ArgumentNullException` if `aggregateRoot` is `null`.
  Throws `InvalidOperationException` if there are no uncommitted events.

## Usage

```csharp
// Example 1: Checking for uncommitted events before saving
var aggregate = new OrderAggregate(orderId);
aggregate.Apply(new OrderCreated(orderId, customerId));
if (AggregateRootExtensions.HasUncommittedEvents(aggregate))
{
    Console.WriteLine($"Pending changes detected. Events to commit: {AggregateRootExtensions.UncommittedEventsCount(aggregate)}");
}

// Example 2: Measuring event age for diagnostics
var diagnostics = new DiagnosticService();
var age = AggregateRootExtensions.GetAge(aggregate);
diagnostics.RecordLatency("OrderPendingMs", age.TotalMilliseconds);
```

## Notes

- All methods are thread-safe for concurrent reads of the same aggregate root instance.
- `GetAge` returns the age of the oldest uncommitted event; if events are appended out of order, the result reflects the earliest timestamp.
- Methods do not mutate the aggregate root state; they only read internal collections.
- Performance is O(1) for `HasUncommittedEvents`, `UncommittedEventsCount`, and `IsModified`; O(n) for `GetAge` where n is the number of uncommitted events.
