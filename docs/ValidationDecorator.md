# ValidationDecorator

The `ValidationDecorator` is a lightweight wrapper that adds validation logic around a command handler in a CQRS/event‑sourcing pipeline. It ensures that incoming commands satisfy defined business rules before they are processed by the underlying handler, throwing a validation exception when any rule is violated.

## API

### `ValidationDecorator(IHandle<TCommand, TResult> inner, BusinessRuleDecorator validator)`

**Purpose**  
Creates a new instance that decorates the supplied `inner` handler with validation performed by `validator`.

**Parameters**  
- `inner`: The handler that will be invoked after validation succeeds. Must not be `null`.  
- `validator`: Provides the business‑rule validation logic. Must not be `null`.

**Return value**  
A new `ValidationDecorator` instance.

**Exceptions**  
- `ArgumentNullException` if `inner` or `validator` is `null`.

---

### `public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)`

**Purpose**  
Validates the command using the encapsulated `BusinessRuleDecorator` and, if validation passes, forwards the command to the inner handler.

**Parameters**  
- `command`: The command to be handled. Must not be `null`.  
- `cancellationToken`: Optional token to observe for cancellation.

**Return value**  
A `Task<TResult>` that completes with the result produced by the inner handler.

**Exceptions**  
- `ArgumentNullException` if `command` is `null`.  
- `ValidationException` (or a derived type) if any business rule fails validation.  
- Any exception thrown by the inner handler is propagated unchanged.

---

### `public BusinessRuleDecorator Validator { get; }`

**Purpose**  
Provides read‑only access to the validation component used by the decorator.

**Return value**  
The `BusinessRuleDecorator` instance supplied at construction time.

**Exceptions**  
None.

---

### `public async Task<TResult> HandleAsync(object command, CancellationToken cancellationToken = default)`

**Purpose**  
Non‑generic overload that allows the decorator to be used through a loosely‑typed handler interface. The command is cast to `TCommand` before validation and handling.

**Parameters**  
- `command`: The command object. Must not be `null` and must be assignable to `TCommand`.  
- `cancellationToken`: Optional token to observe for cancellation.

**Return value**  
A `Task<TResult>` that completes with the result from the inner handler.

**Exceptions**  
- `ArgumentNullException` if `command` is `null`.  
- `InvalidCastException` if `command` cannot be cast to `TCommand`.  
- `ValidationException` if validation fails.  
- Any exception from the inner handler is propagated.

---

### `public static async Task<T> Execute<TCommand, T>(TCommand command, IHandle<TCommand, T> handler, BusinessRuleDecorator validator, CancellationToken cancellationToken = default)`

**Purpose**  
Convenience method that creates a temporary `ValidationDecorator`, validates the command, and invokes the handler, returning the result.

**Parameters**  
- `command`: The command to validate and handle. Must not be `null`.  
- `handler`: The actual handler to execute after validation. Must not be `null`.  
- `validator`: The validation logic to apply. Must not be `null`.  
- `cancellationToken`: Optional token to observe for cancellation.

**Return value**  
A `Task<T>` that completes with the result from `handler`.

**Exceptions**  
- `ArgumentNullException` if any of `command`, `handler`, or `validator` is `null`.  
- `ValidationException` if validation fails.  
- Any exception thrown by `handler` is propagated.

## Usage

### Basic decoration of a command handler

```csharp
public class CreateOrderHandler : IHandle<CreateOrderCommand, OrderResult>
{
    public Task<OrderResult> HandleAsync(CreateOrderCommand cmd, CancellationToken ct = default)
    {
        // persist the order …
        return Task.FromResult(new OrderResult { OrderId = Guid.NewGuid() });
    }
}

// somewhere in composition root
var inner = new CreateOrderHandler();
var validator = new BusinessRuleDecorator(); // configured with rules for CreateOrderCommand
var decorated = new ValidationDecorator(inner, validator);

// usage
var result = await decorated.HandleAsync(new CreateOrderCommand { /* … */ });
```

### Using the static helper for one‑off validation

```csharp
public async Task<OrderResult> PlaceOrderAsync(CreateOrderCommand cmd)
{
    var validator = new BusinessRuleDecorator(); // pre‑configured
    return await ValidationDecorator.Execute(
        cmd,
        new CreateOrderHandler(),
        validator,
        CancellationToken.None);
}
```

## Notes

- The decorator does **not** mutate the command object; validation is read‑only.  
- If the `BusinessRuleDecorator` throws during validation, the inner handler is never invoked.  
- All instance members are safe to call concurrently from multiple threads provided that the supplied `inner` handler and `validator` are themselves thread‑safe. The decorator itself holds no mutable state after construction.  
- The static `Execute` method creates a new decorator on each call; it incurs minimal allocation overhead but does not reuse validator or handler instances across calls.  
- Passing `null` for any required argument results in an `ArgumentNullException` before any validation logic runs.  
- The non‑generic `HandleAsync(object command, …)` overload performs a runtime cast; misuse (e.g., passing an incompatible command type) results in an `InvalidCastException`. It is intended for scenarios where the decorator is accessed through a non‑generic handler interface.
