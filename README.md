// existing content ...

## DateTimeExtensions

The DateTimeExtensions type provides a set of static methods for manipulating DateTime objects. These methods include ensuring a DateTime is in UTC, rounding to the nearest second, minute, or hour, truncating to midnight, checking if a DateTime is in the past, future, or today, calculating the age in years, and converting to various string formats.

Example usage:
```csharp
var now = DateTime.Now;
var utcNow = DateTimeExtensions.EnsureUtc(now);
var startOfDay = DateTimeExtensions.StartOfDay(now);
var endOfMonth = DateTimeExtensions.EndOfMonth(now);
var age = DateTimeExtensions.AgeInYears(now.AddYears(-30));
var iso8601 = DateTimeExtensions.ToIso8601(now);
```

## SerializationUtilities

The `SerializationUtilities` class provides static methods for JSON serialization and deserialization using System.Text.Json. It handles common serialization challenges like dates, decimals, and null values, and provides consistent formatting across the application. The class is thread-safe and caches JsonSerializerOptions for performance.

Example usage:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        // Create a sample object
        var person = new { Name = "Alice", Age = 30, Active = true };
        
        // Serialize to compact JSON
        var json = SerializationUtilities.ToJson(person);
        Console.WriteLine(json);
        // Output: {"name":"Alice","age":30,"active":true}
        
        // Serialize to pretty-printed JSON
        var prettyJson = SerializationUtilities.ToJson(person, prettyPrint: true);
        
        // Deserialize back to object
        var deserialized = SerializationUtilities.FromJson<dynamic>(json);
        Console.WriteLine(deserialized.Name); // Output: Alice
        
        // Deep clone an object
        var clone = SerializationUtilities.DeepClone(person);
        
        // Convert object to dictionary
        var dict = SerializationUtilities.Todictionary(person);
        
        // Merge JSON patch into existing object
        var updated = SerializationUtilities.MergeJson(person, "{\"age\":31}");
    }
}
```

The `ReflectionUtilities` class provides a set of static methods for working with reflection, including finding types that implement a specific interface, getting public properties of a type, finding methods by name and parameter count, and more. It uses caching to improve performance.

### Usage Example

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        // Find all types that implement IEventHandler
        var eventHandlers = ReflectionUtilities.GetTypesImplementing<IEventHandler>();
        foreach (var handler in eventHandlers)
        {
            Console.WriteLine(handler.Name);
        }

        // Get public properties of a type
        var properties = ReflectionUtilities.GetPublicProperties(typeof(MyType));
        foreach (var property in properties)
        {
            Console.WriteLine(property.Name);
        }

        // Find a method by name and parameter count
        var method = ReflectionUtilities.FindMethod(typeof(MyType), "MyMethod", 2);
        if (method != null)
        {
            Console.WriteLine(method.Name);
        }
    }
}
```

