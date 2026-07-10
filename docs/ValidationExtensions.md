# ValidationExtensions
The `ValidationExtensions` class provides a set of static methods for validating various types of data, including objects, strings, decimals, and collections. These methods can be used to ensure that data meets certain criteria, such as being non-null, non-empty, or within a specific range. By using these methods, developers can write more robust and reliable code that is less prone to errors and exceptions.

## API
* `public static T NotNull<T>(this T value)`: Ensures that the provided value is not null. If the value is null, it throws an `ArgumentNullException`.
* `public static string NotNullOrEmpty(this string value)`: Ensures that the provided string is not null or empty. If the value is null or empty, it throws an `ArgumentException`.
* `public static decimal NotNegative(this decimal value)`: Ensures that the provided decimal value is not negative. If the value is negative, it throws an `ArgumentException`.
* `public static decimal InRange(this decimal value, decimal min, decimal max)`: Ensures that the provided decimal value is within the specified range. If the value is outside the range, it throws an `ArgumentException`.
* `public static string ValidGuid(this string value)`: Ensures that the provided string is a valid GUID. If the value is not a valid GUID, it throws an `ArgumentException`.
* `public static string ValidEmail(this string value)`: Ensures that the provided string is a valid email address. If the value is not a valid email address, it throws an `ArgumentException`.
* `public static IEnumerable<T> NotEmpty<T>(this IEnumerable<T> value)`: Ensures that the provided collection is not empty. If the collection is empty, it throws an `ArgumentException`.
* `public static string MaxLength(this string value, int maxLength)`: Ensures that the provided string does not exceed the specified maximum length. If the string exceeds the maximum length, it throws an `ArgumentException`.
* `public static string MinLength(this string value, int minLength)`: Ensures that the provided string meets the specified minimum length. If the string is shorter than the minimum length, it throws an `ArgumentException`.
* `public static void Ensure(bool condition, string errorMessage)`: Ensures that the provided condition is true. If the condition is false, it throws an `ArgumentException` with the specified error message.
* `public static (bool IsValid, string? ErrorMessage) ValidateRequired(this string value)`: Validates that the provided string is not null or empty. Returns a tuple containing a boolean indicating whether the string is valid and an error message if it is not.
* `public static (bool IsValid, string? ErrorMessage) ValidateRange(this decimal value, decimal min, decimal max)`: Validates that the provided decimal value is within the specified range. Returns a tuple containing a boolean indicating whether the value is valid and an error message if it is not.

## Usage
```csharp
// Example 1: Validating a string
string email = "user@example.com";
if (email.ValidateRequired().IsValid)
{
    Console.WriteLine("Email is valid");
}
else
{
    Console.WriteLine("Email is not valid");
}

// Example 2: Validating a decimal value
decimal price = 10.99m;
if (price.ValidateRange(0, 100).IsValid)
{
    Console.WriteLine("Price is valid");
}
else
{
    Console.WriteLine("Price is not valid");
}
```

## Notes
The `ValidationExtensions` class is designed to be thread-safe, as all methods are static and do not rely on any shared state. However, it is still important to note that the validation methods may throw exceptions if the provided data is invalid. In addition, some methods may have edge cases that need to be considered, such as the `ValidGuid` method which may not work correctly for GUIDs in certain formats. It is also worth noting that the `Ensure` method can be used to validate any condition, not just those related to data validation.
