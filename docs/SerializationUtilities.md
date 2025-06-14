# SerializationUtilities

The `SerializationUtilities` class provides a static set of helper methods for handling JSON serialization, deserialization, object cloning, and data merging within the `dotnet-cqrs-eventsourcing` project. It acts as a centralized abstraction over the underlying JSON serializer (typically `System.Text.Json`), offering generic and non-generic entry points to convert between CLR objects and JSON representations or dictionary structures. This utility is designed to streamline event payload handling, state restoration, and data transformation tasks common in Event Sourcing and CQRS architectures.

## API

### `ToJson<T>`
Serializes a specified object of type `T` into a JSON string.
*   **Parameters**: `T value` – The object instance to serialize.
*   **Return Value**: `string` – The JSON representation of the input object.
*   **Exceptions**: Throws a serialization exception if the object graph contains circular references, unsupported types, or if the serializer is misconfigured.

### `FromJson<T>`
Deserializes a JSON string into an object of type `T`.
*   **Parameters**: `string json` – The JSON string to deserialize.
*   **Return Value**: `T?` – The deserialized object instance, or `null` if the input JSON represents a null value.
*   **Exceptions**: Throws a deserialization exception if the JSON is malformed, does not match the structure of `T`, or contains invalid data types for the target properties.

### `FromJson`
Deserializes a JSON string into an object without specifying the generic type at compile time, returning a generic `object`.
*   **Parameters**: `string json`, `Type type` – The JSON string and the target `System.Type` to deserialize into.
*   **Return Value**: `object?` – The deserialized object instance cast as `object`, or `null` if the input JSON represents a null value.
*   **Exceptions**: Throws a deserialization exception if the JSON is invalid or cannot be mapped to the provided `type`.

### `ToDictionary<T>`
Converts an object of type `T` into a flat or nested dictionary representation where keys are property names and values are the corresponding property values.
*   **Parameters**: `T value` – The object instance to convert.
*   **Return Value**: `Dictionary<string, object?>` – A dictionary containing the public properties of the object.
*   **Exceptions**: May throw if the object contains properties that cannot be resolved into dictionary entries or if serialization intermediates fail.

### `DeepClone<T>`
Creates a deep copy of an object by serializing it to JSON and immediately deserializing it back into a new instance.
*   **Parameters**: `T value` – The object instance to clone.
*   **Return Value**: `T?` – A new instance of `T` with identical data values, or `null` if the input is `null`.
*   **Exceptions**: Throws if the object cannot be fully serialized and deserialized (e.g., due to unsupported types or circular references).

### `MergeJson<T>`
Merges a base object of type `T` with additional JSON data, where the JSON values override existing properties on the base object.
*   **Parameters**: `T baseObject`, `string json` – The initial object instance and the JSON string containing override values.
*   **Return Value**: `T?` – A new instance of `T` resulting from the merge, or `null` if inputs result in a null outcome.
*   **Exceptions**: Throws if the JSON is malformed or if property types in the JSON conflict irreconcilably with the target type `T`.

## Usage

### Example 1: Event Payload Serialization and Deserialization
This example demonstrates storing an event payload as a JSON string and retrieving it later as a strongly typed object.

```csharp
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Serialize the event for storage
var eventPayload = new OrderCreatedEvent 
{ 
    OrderId = Guid.NewGuid(), 
    TotalAmount = 99.95m, 
    CreatedAt = DateTime.UtcNow 
};

string jsonStorage = SerializationUtilities.ToJson(eventPayload);

// Later, deserialize from storage
OrderCreatedEvent? restoredEvent = SerializationUtilities.FromJson<OrderCreatedEvent>(jsonStorage);

if (restoredEvent != null)
{
    Console.WriteLine($"Restored Order: {restoredEvent.OrderId}");
}
```

### Example 2: Deep Cloning and Merging Updates
This example shows how to create a safe copy of a state object and then apply a partial update received as JSON.

```csharp
public class UserState
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int LoginCount { get; set; }
}

var currentState = new UserState 
{ 
    Username = "jdoe", 
    Email = "jdoe@example.com", 
    LoginCount = 5 
};

// Create a deep clone to ensure immutability of the original reference
UserState? workingCopy = SerializationUtilities.DeepClone(currentState);

// Define a partial update (only changing Email)
string updateJson = @"{ ""Email"": ""new.email@example.com"" }";

// Merge the update into the working copy
UserState? updatedState = SerializationUtilities.MergeJson(workingCopy, updateJson);

// updatedState now contains the new Email, while preserving Username and LoginCount
```

## Notes

*   **Null Handling**: All methods returning nullable types (`T?` or `object?`) will return `null` if the input JSON explicitly represents a null value or if the input object provided for serialization/cloning is `null`. Callers should perform null checks before accessing members of the return value.
*   **Thread Safety**: As the class exposes only static methods that operate on input parameters without maintaining internal mutable state, `SerializationUtilities` is inherently thread-safe for concurrent calls, assuming the underlying JSON serializer implementation is also thread-safe (which is standard for `System.Text.Json`).
*   **Reference Semantics**: The `DeepClone` method breaks reference equality. The returned object is a distinct instance in memory; changes to the clone will not affect the original object.
*   **Merge Behavior**: The `MergeJson` method performs a shallow merge at the property level. If a property is a complex object, the entire property value from the JSON string typically replaces the existing property value in the base object rather than recursively merging nested properties, depending on the specific serializer configuration.
*   **Type Compatibility**: When using the non-generic `FromJson` method, ensure the provided `Type` argument matches the structure of the JSON string; otherwise, runtime casting errors or deserialization exceptions will occur upon accessing specific properties.
