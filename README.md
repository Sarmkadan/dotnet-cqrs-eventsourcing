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

## GuardClauses

The `GuardClauses` class provides static methods for validating method arguments and enforcing preconditions. It helps prevent invalid inputs by throwing descriptive exceptions early, ensuring robust and maintainable code. Methods include null checks, range validation, format validation, and more.

Example usage:
```csharp
public void ProcessOrder(Order order, string customerName, int quantity, decimal price, Guid orderId, string email)
{
    GuardClauses.NotNull(order, nameof(order));
    GuardClauses.NotNullOrEmpty(customerName, nameof(customerName));
    GuardClauses.InRange(quantity, 1, 100, nameof(quantity));
    GuardClauses.NotNegative(price, nameof(price));
    GuardClauses.NotZero(orderId, nameof(orderId));
    GuardClauses.Condition(email.Contains("@"), "Email must contain '@'");
    GuardClauses.Matches(email, @"^\w+@[a-zA-Z_]+?\.[a-zA-Z]{2,3}$", nameof(email));
}
```

## CqrsHelpers

The `CqrsHelpers` class provides static utilities for CQRS command/event routing, handler discovery, and type mapping. It enables dynamic discovery of command and event handlers, validates commands, extracts aggregate IDs, and maintains type mappings for event deserialization. The class uses thread-safe caching for performance and supports clearing caches when assemblies change.

Example usage:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        // Discover all command handlers in the current assembly
        var commandHandlers = CqrsHelpers.GetCommandHandlers(typeof(Program).Assembly);
        Console.WriteLine($"Found {commandHandlers.Count()} command handlers");
        
        // Discover all event handlers in the current assembly
        var eventHandlers = CqrsHelpers.GetEventHandlers(typeof(Program).Assembly);
        Console.WriteLine($"Found {eventHandlers.Count()} event handlers");
        
        // Get metadata about a command type
        var commandType = typeof(CreateProductCommand);
        var metadata = CqrsHelpers.GetHandlerMetadata(commandType);
        Console.WriteLine($"Command: {metadata.DisplayName}");
        Console.WriteLine($"Properties: {string.Join(", ", metadata.Properties.Select(p => p.Name))}");
        
        // Register an event type for later deserialization
        CqrsHelpers.RegisterEventType(typeof(ProductCreatedEvent));
        
        // Resolve an event type by name
        var resolvedType = CqrsHelpers.ResolveEventType("ProductCreatedEvent");
        Console.WriteLine($"Resolved event type: {resolvedType?.Name}");
        
        // Extract aggregate ID from a command
        var command = new CreateProductCommand
        {
            AggregateId = Guid.NewGuid().ToString(),
            Name = "New Product",
            Price = 99.99m
        };
        
        var aggregateId = CqrsHelpers.ExtractAggregateId(command);
        Console.WriteLine($"Aggregate ID: {aggregateId}");
        
        // Get the target aggregate type from a command
        var targetType = CqrsHelpers.GetTargetAggregateType(commandType);
        Console.WriteLine($"Target aggregate type: {targetType?.Name}");
        
        // Validate a command
        var validationErrors = CqrsHelpers.ValidateCommand(command);
        if (validationErrors.Any())
        {
            Console.WriteLine("Validation errors:");
            foreach (var error in validationErrors)
            {
                Console.WriteLine($"- {error}");
            }
        }
        else
        {
            Console.WriteLine("Command is valid!");
        }
        
        // Clear caches (useful in tests)
        CqrsHelpers.ClearCaches();
    }
}

// Example command and event types
public class CreateProductCommand
{
    public string AggregateId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class ProductCreatedEvent
{
    public string AggregateId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## PagedResult

The `PagedResult<T>` class represents a paginated result set that includes the items for the current page along with pagination metadata. It's designed to efficiently handle large datasets by splitting results into manageable pages, preventing full dataset loads into memory.

### Members
- `List<T> Items` - The items for the current page
- `int PageNumber` - The current page number (1-based)
- `int PageSize` - The number of items per page
- `long TotalCount` - The total number of items across all pages
- `int TotalPages` - The total number of pages
- `bool HasPreviousPage` - Whether there's a previous page
- `bool HasNextPage` - Whether there's a next page

### Usage Example

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        // Sample data
        var products = Enumerable.Range(1, 100)
            .Select(i => new Product { Id = i, Name = $"Product {i}" })
            .ToList();

        // Paginate using IEnumerable
        var pagedResult = products.ToPagedResult(pageNumber: 2, pageSize: 10);

        Console.WriteLine($"Page {pagedResult.PageNumber} of {pagedResult.TotalPages}");
        Console.WriteLine($"Items on page: {pagedResult.Items.Count}");
        Console.WriteLine($"Total items: {pagedResult.TotalCount}");
        Console.WriteLine($"Has previous page: {pagedResult.HasPreviousPage}");
        Console.WriteLine($"Has next page: {pagedResult.HasNextPage}");

        // Paginate using IQueryable (database-level pagination)
        var dbContext = new AppDbContext();
        var query = dbContext.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name);

        var queryResult = query.ToPagedResult(pageNumber: 1, pageSize: 20);

        // Convert to API-friendly format
        var apiResponse = queryResult.ToApiResponse();
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}
```

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

