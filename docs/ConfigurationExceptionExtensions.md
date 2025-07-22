# ConfigurationExceptionExtensions

Provides factory methods for creating `ConfigurationException` instances with structured context, validation details, and suggestions. These static methods simplify the construction of configuration-related exceptions in a consistent manner, reducing boilerplate and improving error reporting.

## API

### `WithContext`
```csharp
public static ConfigurationException WithContext(ConfigurationException exception, object context)
```
Attaches a context object to an existing `ConfigurationException`.  
**Parameters**  
- `exception` – The original configuration exception to extend.  
- `context` – An object representing the execution context (e.g., a configuration section name, environment, or custom data).  

**Returns**  
A new `ConfigurationException` that wraps the original exception and includes the provided context.  

**Throws**  
- `ArgumentNullException` if `exception` is `null`.

### `MissingWithSuggestion`
```csharp
public static ConfigurationException MissingWithSuggestion(string key, string suggestion)
```
Creates a `ConfigurationException` indicating that a required configuration key is missing, along with a suggested fix.  
**Parameters**  
- `key` – The name of the missing configuration key.  
- `suggestion` – A human-readable suggestion for resolving the missing key (e.g., a default value or alternative key).  

**Returns**  
A `ConfigurationException` with a message that includes both the missing key and the suggestion.  

**Throws**  
- `ArgumentNullException` if `key` is `null`.  
- `ArgumentException` if `key` is empty or consists only of white space.

### `InvalidWithDetails`
```csharp
public static ConfigurationException InvalidWithDetails(string key, object value, string details)
```
Creates a `ConfigurationException` for an invalid configuration value, including the offending key, the actual value, and a description of the validation failure.  
**Parameters**  
- `key` – The configuration key whose value is invalid.  
- `value` – The actual (invalid) value that was provided.  
- `details` – A description of why the value is invalid (e.g., expected format, range, or type).  

**Returns**  
A `ConfigurationException` whose message contains the key, the value, and the details.  

**Throws**  
- `ArgumentNullException` if `key` or `details` is `null`.  
- `ArgumentException` if `key` is empty or white space.

### `FromValidationErrors`
```csharp
public static ConfigurationException FromValidationErrors(IEnumerable<ValidationError> errors)
```
Creates a `ConfigurationException` from a collection of validation errors, aggregating them into a single exception.  
**Parameters**  
- `errors` – A sequence of `ValidationError` objects (each typically containing a key, a message, and optionally a value).  

**Returns**  
A `ConfigurationException` that includes all validation errors in its message.  

**Throws**  
- `ArgumentNullException` if `errors` is `null`.  
- `ArgumentException` if `errors` is empty.

### `MissingMultiple`
```csharp
public static ConfigurationException MissingMultiple(IEnumerable<string> keys)
```
Creates a `ConfigurationException` for multiple missing configuration keys.  
**Parameters**  
- `keys` – A collection of key names that are missing.  

**Returns**  
A `ConfigurationException` whose message lists all missing keys.  

**Throws**  
- `ArgumentNullException` if `keys` is `null`.  
- `ArgumentException` if `keys` is empty.

## Usage

### Example 1: Reporting a missing key with a suggestion and attaching context

```csharp
try
{
    var connectionString = ConfigurationManager.AppSettings["DatabaseConnection"];
    if (string.IsNullOrEmpty(connectionString))
    {
        var missingEx = ConfigurationExceptionExtensions.MissingWithSuggestion(
            "DatabaseConnection",
            "Add a 'DatabaseConnection' key to appSettings or use environment variable 'DB_CONN'."
        );
        throw ConfigurationExceptionExtensions.WithContext(
            missingEx,
            new { Environment = "Production", Section = "appSettings" }
        );
    }
}
catch (ConfigurationException ex)
{
    // ex.Message includes the missing key, suggestion, and context
    Logger.LogError(ex);
}
```

### Example 2: Aggregating validation errors and reporting an invalid value

```csharp
var errors = new List<ValidationError>
{
    new ValidationError("TimeoutSeconds", "Must be a positive integer", "abc"),
    new ValidationError("MaxRetries", "Must be between 1 and 10", 15)
};

if (errors.Any())
{
    var validationEx = ConfigurationExceptionExtensions.FromValidationErrors(errors);
    // Further enrich with an invalid value detail
    var detailedEx = ConfigurationExceptionExtensions.InvalidWithDetails(
        "TimeoutSeconds",
        "abc",
        "Expected an integer value."
    );
    throw new AggregateException(validationEx, detailedEx);
}
```

## Notes

- All methods are static and thread‑safe; they do not modify any shared state and can be called concurrently without synchronization.  
- Passing `null` for any required string or collection parameter results in an `ArgumentNullException`.  
- Empty or white‑space strings for `key` in `MissingWithSuggestion` and `InvalidWithDetails` cause an `ArgumentException`.  
- `MissingMultiple` and `FromValidationErrors` throw `ArgumentException` when the provided collection is empty, as an exception with no missing keys or no errors is semantically invalid.  
- The `WithContext` method is designed to wrap an existing `ConfigurationException`; passing a non‑`ConfigurationException` instance is not supported and will throw `ArgumentNullException` if `null`, but no other type checking is performed.  
- The `ValidationError` type referenced by `FromValidationErrors` is expected to be a simple data class with at least `Key`, `Message`, and optionally `Value` properties. Its exact definition is part of the project’s validation infrastructure.
