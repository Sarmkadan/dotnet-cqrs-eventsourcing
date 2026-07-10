# IJsonFormatter
The `IJsonFormatter` type is designed to provide a standardized way of formatting and parsing JSON data in C# applications, particularly in the context of event sourcing and CQRS (Command Query Responsibility Segregation) architectures. It offers methods for formatting objects and collections into JSON strings, parsing JSON strings back into objects, and configuring serialization options.

## API
* `public string Format<T>(T obj)`: Formats an object of type `T` into a JSON string. The `obj` parameter is the object to be formatted. The method returns a JSON string representation of the object. It may throw exceptions if the object cannot be serialized.
* `public string FormatCollection<T>(IEnumerable<T> collection)`: Formats a collection of objects of type `T` into a JSON string. The `collection` parameter is the collection to be formatted. The method returns a JSON string representation of the collection. It may throw exceptions if the collection or any of its items cannot be serialized.
* `public T? Parse<T>(string json)`: Parses a JSON string into an object of type `T`. The `json` parameter is the JSON string to be parsed. The method returns the parsed object, or `null` if the parsing fails. It may throw exceptions if the JSON string is malformed or cannot be deserialized into the specified type.
* `public JsonSerializerOptions GetOptions`: Gets the current `JsonSerializerOptions` used for serialization and deserialization. This property allows for customization of the serialization process.
* `public bool PrettyPrint`: Gets or sets a value indicating whether the formatted JSON should be pretty-printed (i.e., formatted with indentation and line breaks for readability).
* `public bool IgnoreNulls`: Gets or sets a value indicating whether null values should be ignored during serialization.
* `public override DateTime Read`: Reads a `DateTime` value from the underlying data source. This method is part of the interface's override for handling specific data types.
* `public override void Write`: Writes a value to the underlying data source. This method is part of the interface's override for handling specific data types.
* `public override decimal Read`: Reads a `decimal` value from the underlying data source. This method is part of the interface's override for handling specific data types.
* `public override void Write`: Writes a value to the underlying data source. This method is part of the interface's override for handling specific data types.
* `public static IServiceCollection AddJsonFormatter`: Adds the JSON formatter to the specified `IServiceCollection`. This method is used for registering the formatter in a dependency injection container.

## Usage
The following examples demonstrate how to use the `IJsonFormatter` interface for formatting and parsing JSON data:
```csharp
// Example 1: Formatting an object
var formatter = new JsonFormatter(); // Assuming JsonFormatter implements IJsonFormatter
var person = new Person { Name = "John Doe", Age = 30 };
var json = formatter.Format(person);
Console.WriteLine(json); // Output: {"Name":"John Doe","Age":30}

// Example 2: Parsing a JSON string
var json = "{\"Name\":\"Jane Doe\",\"Age\":25}";
var formatter = new JsonFormatter(); // Assuming JsonFormatter implements IJsonFormatter
var person = formatter.Parse<Person>(json);
Console.WriteLine(person.Name); // Output: Jane Doe
Console.WriteLine(person.Age); // Output: 25
```

## Notes
When using the `IJsonFormatter` interface, consider the following:
- The `PrettyPrint` and `IgnoreNulls` properties can significantly affect the output and performance of the serialization process.
- The `GetOptions` property allows for fine-grained control over the serialization options, including handling of null values, circular references, and more.
- The `Read` and `Write` override methods suggest that the formatter may be used in a streaming or data access context, where efficient handling of specific data types is crucial.
- Thread-safety should be considered when implementing or using the `IJsonFormatter` interface, especially if the formatter is used in a multi-threaded environment or as a singleton instance. Ensure that the implementation is thread-safe to avoid unexpected behavior or data corruption.
