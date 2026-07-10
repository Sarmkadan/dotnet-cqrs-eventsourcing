# ConfigurationException

`ConfigurationException` is a specialized exception type used to signal configuration-related errors in applications built with the dotnet-cqrs-eventsourcing framework. It provides strongly-typed constructors and factory methods for common configuration failure scenarios, ensuring consistent error reporting and easier diagnostics.

## API

### `public ConfigurationException()`

Initializes a new instance of the `ConfigurationException` class with a default error message.

### `public ConfigurationException(string message)`

Initializes a new instance of the `ConfigurationException` class with a custom error message.

**Parameters**
- `message`: The message that describes the error.

### `public static ConfigurationException MissingRequiredConfiguration(string key)`

Creates a `ConfigurationException` indicating that a required configuration value is missing.

**Parameters**
- `key`: The configuration key that was not found.

**Returns**
A new `ConfigurationException` instance with a message indicating the missing key.

### `public static ConfigurationException InvalidConfigurationValue(string key, string value)`

Creates a `ConfigurationException` indicating that a configuration value is invalid.

**Parameters**
- `key`: The configuration key with the invalid value.
- `value`: The invalid value that was provided.

**Returns**
A new `ConfigurationException` instance with a message indicating the invalid value.

### `public static ConfigurationException ValidationFailed(string message)`

Creates a `ConfigurationException` indicating that configuration validation failed.

**Parameters**
- `message`: The validation failure message.

**Returns**
A new `ConfigurationException` instance with the provided validation message.

## Usage
