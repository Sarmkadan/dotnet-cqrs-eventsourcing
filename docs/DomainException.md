# DomainException

A specialized exception type used in domain-driven design to represent business rule violations or domain-specific errors. It carries a machine-readable `Code` for client applications to handle errors programmatically, and a mutable `Metadata` dictionary for attaching additional context such as entity identifiers, timestamps, or correlation IDs.

## API

### `string Code`
A read-only property representing the error code associated with the exception. This code is typically used by clients to identify and handle specific domain errors consistently.

### `Dictionary<string, object> Metadata`
A mutable dictionary containing additional contextual data about the exception. This can include values such as entity IDs, timestamps, or other diagnostic information relevant to the domain error.

### `DomainException()`
Constructs a new instance of the `DomainException` with default values. The `Code` is set to an empty string, and the `Metadata` dictionary is initialized as empty.

### `DomainException(string code)`
Constructs a new instance of the `DomainException` with the specified error `code`. The `Metadata` dictionary is initialized as empty.

**Parameters**
- `code` (string): The error code representing the domain-specific error.

### `DomainException WithMetadata(Dictionary<string, object> metadata)`
Returns a new `DomainException` instance with the same `Code` as the current instance, but with the provided `metadata` merged into the existing `Metadata` dictionary. If the same key exists in both dictionaries, the value from the provided `metadata` takes precedence.

**Parameters**
- `metadata` (Dictionary<string, object>): A dictionary of additional context to attach to the exception.

**Returns**
- `DomainException`: A new instance with updated metadata.

### `override string ToString()`
Returns a string representation of the exception, including the `Code` and a summary of the `Metadata` contents. The format is implementation-defined and may change between versions.

**Returns**
- `string`: A human-readable representation of the exception.

## Usage
