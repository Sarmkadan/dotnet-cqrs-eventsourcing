# RateLimitingMiddlewareJsonExtensions

Provides JSON serialization and deserialization functionality for rate limiting middleware state management in CQRS and event sourcing scenarios. Enables persistence and restoration of token bucket configurations and states through JSON representation.

## API

### ToJson

Serializes the current rate limiting state to a JSON string representation.

**Parameters:**
- `state` (`RateLimitingState`): The state object to serialize.

**Returns:**  
`string` - A JSON string representing the serialized state.

**Exceptions:**  
Throws `System.Text.Json.JsonException` if serialization fails due to invalid state data.

---

### FromJson

Deserializes a JSON string into a `RateLimitingState` instance.

**Parameters:**  
- `json` (`string`): The JSON string to deserialize.

**Returns:**  
`RateLimitingState?` - The deserialized state object, or `null` if deserialization fails.

**Exceptions:**  
Throws `System.Text.Json.JsonException` if the JSON is malformed or incompatible with expected structure.

---

### TryFromJson

Attempts to deserialize a JSON string into a `RateLimitingState` instance without throwing exceptions.

**Parameters:**  
- `json` (`string`): The JSON string to deserialize.  
- `state` (`out RateLimitingState`): The output parameter receiving the deserialized state.

**Returns:**  
`bool` - `true` if deserialization succeeded; `false` otherwise.

**Exceptions:**  
Does not throw exceptions. Returns `false` for invalid or incompatible JSON input.

---

### Options

Gets or sets the rate limiting configuration options.

**Type:**  
`RateLimitOptions?`

**Remarks:**  
Contains settings such as maximum tokens, refill rate, and time window definitions. May be `null` if not configured.

---

### BucketState

Gets or sets the collection of token bucket states keyed by unique identifiers.

**Type:**  
`Dictionary<string, TokenBucketState>?`

**Remarks:**  
Each entry represents the current token count and metadata for a specific rate-limited resource. May be `null` if no buckets have been initialized.

---

### Tokens

Gets or sets the current number of available tokens in the primary bucket.

**Type:**  
`double`

**Remarks:**  
Represents the instantaneous token balance. Value is typically between 0 and `MaxTokens`.

---

### MaxTokens

Gets or sets the maximum token capacity for the primary bucket.

**Type:**  
`double`

**Remarks:**  
Defines the upper limit for token accumulation. Used to calculate refill thresholds.

---

### TokensPerSecond

Gets or sets the rate at which tokens are replenished.

**Type:**  
`double`

**Remarks:**  
Controls the refill frequency. A value of 10 means 10 tokens are added per second.

---

### LastRefillTime

Gets or sets the timestamp of the last token refill operation.

**Type:**  
`DateTime`

**Remarks:**  
Used to determine elapsed time since last refill for calculating current token levels.

---

### LastAccessTime

Gets or sets the timestamp of the most recent rate-limited request.

**Type:**  
`DateTime`

**Remarks:**  
Tracks activity for idle timeout or cleanup logic.

---

## Usage

### Serializing and Deserializing Rate Limiting State

```csharp
// Serialize current state to JSON
var state = new RateLimitingState 
{
    MaxTokens = 100,
    TokensPerSecond = 10,
    Tokens = 50,
    LastRefillTime = DateTime.UtcNow
};
string json = RateLimitingMiddlewareJsonExtensions.ToJson(state);

// Restore state from JSON
var restoredState = RateLimitingMiddlewareJsonExtensions.FromJson(json);
Console.WriteLine($"Restored tokens: {restoredState?.Tokens}");
```

### Safely Parsing JSON with TryFromJson

```csharp
string inputJson = "{\"MaxTokens\":50,\"TokensPerSecond\":5,\"Tokens\":25}";
if (RateLimitingMiddlewareJsonExtensions.TryFromJson(inputJson, out var parsedState))
{
    Console.WriteLine("Parsed successfully");
    // Use parsedState
}
else
{
    Console.WriteLine("Invalid JSON format");
}
```

---

## Notes

- **Null Handling**: `FromJson` returns `null` for invalid input, while `TryFromJson` provides a non-throwing alternative with explicit success/failure signaling.
- **Thread Safety**: The `BucketState` dictionary and timestamp properties are not thread-safe. Concurrent modifications during serialization/deserialization may result in inconsistent state. External synchronization is required in multi-threaded contexts.
- **Precision Loss**: Floating-point properties (`Tokens`, `MaxTokens`, `TokensPerSecond`) may lose precision during JSON round-trips due to IEEE 754 representation limitations.
- **DateTime Serialization**: Timestamps use system-default serialization formats. Cross-platform compatibility may require explicit `DateTime` format specifiers in `ToJson`/`FromJson` implementations.
