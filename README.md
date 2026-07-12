// existing content ...

## ConfigurationExceptionExtensions

`ConfigurationExceptionExtensions` provides a set of extension methods for `ConfigurationException` to enhance error handling and provide more context to configuration-related errors. These extensions facilitate better error messages and suggestions for common configuration mistakes.

### Usage Examples

```csharp
try
{
    // Attempt to load configuration
    var config = configurationBuilder.Build();
}
catch (ConfigurationException ex)
{
    // Add context to the exception
    var detailedException = ex.WithContext("Failed to load application configuration");

    // Handle the exception or rethrow with more details
    Console.WriteLine(detailedException.Message);
}

// Attempt to get a missing setting
try
{
    var value = configuration["MissingSetting"];
    if (string.IsNullOrEmpty(value))
    {
        throw ConfigurationException.MissingWithSuggestion("MissingSetting", "Check appsettings.json for the setting.");
    }
}
catch (ConfigurationException ex)
{
    Console.WriteLine(ex.Message);
}

// Invalid configuration
try
{
    // Simulate an invalid configuration operation
    throw ConfigurationException.InvalidWithDetails("Invalid configuration", "The configuration is not valid.");
}
catch (ConfigurationException ex)
{
    Console.WriteLine(ex.Message);
}

// From validation errors
try
{
    // Simulate validation errors
    var validationErrors = new[] { "Error1", "Error2" };
    throw ConfigurationException.FromValidationErrors(validationErrors);
}
catch (ConfigurationException ex)
{
    Console.WriteLine(ex.Message);
}

// Missing multiple settings
try
{
    // Simulate missing multiple settings
    throw ConfigurationException.MissingMultiple(new[] { "Setting1", "Setting2" });
}
catch (ConfigurationException ex)
{
    Console.WriteLine(ex.Message);
}
```