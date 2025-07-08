# Money

The `Money` type represents a monetary value composed of a decimal `Amount` and a string `Currency` identifier. It provides arithmetic operations, comparison operators, and equality semantics for working with currency-denominated values in the `dotnet-cqrs-eventsourcing` domain.

## API

### `public decimal Amount`

Gets or sets the numeric amount of the money. This field is mutable.

### `public string Currency`

Gets or sets the currency code (e.g., `"USD"`, `"EUR"`). This field is mutable.

### `public Money`

Initializes a new instance of the `Money` type. The exact parameter list is not exposed in the public API; the constructor is used to create a `Money` value with an initial `Amount` and `Currency`.

### `public Money Add(Money other)`

Returns a new `Money` whose amount is the sum of the current instance and `other`, and whose currency is the same as the current instance.

- **Parameters**: `other` – a `Money` instance to add.
- **Returns**: A new `Money` with the summed amount.
- **Throws**: `InvalidOperationException` if the currency of `other` does not match the current instance’s currency.

### `public Money Subtract(Money other)`

Returns a new `Money` whose amount is the difference between the current instance and `other`, and whose currency is the same as the current instance.

- **Parameters**: `other` – a `Money` instance to subtract.
- **Returns**: A new `Money` with the subtracted amount.
- **Throws**: `InvalidOperationException` if the currency of `other` does not match the current instance’s currency.

### `public bool IsGreaterThan(Money other)`

Compares the current instance to `other` and returns `true` if the current amount is greater than the amount of `other`, assuming the same currency.

- **Parameters**: `other` – a `Money` instance to compare.
- **Returns**: `true` if the current amount is greater; otherwise `false`.
- **Throws**: `InvalidOperationException` if the currencies differ.

### `public bool IsLessThan(Money other)`

Compares the current instance to `other` and returns `true` if the current amount is less than the amount of `other`, assuming the same currency.

- **Parameters**: `other` – a `Money` instance to compare.
- **Returns**: `true` if the current amount is less; otherwise `false`.
- **Throws**: `InvalidOperationException` if the currencies differ.

### `public bool Equals(Money other)`

Determines whether the current instance is equal to another `Money` object by comparing both `Amount` and `Currency`.

- **Parameters**: `other` – a `Money` instance to compare.
- **Returns**: `true` if both `Amount` and `Currency` are equal; otherwise `false`.

### `public override bool Equals(object obj)`

Determines whether the current instance is equal to the specified object. If `obj` is a `Money`, delegates to the strongly-typed `Equals(Money)` method.

- **Parameters**: `obj` – an object to compare.
- **Returns**: `true` if `obj` is a `Money` and equal; otherwise `false`.

### `public override int GetHashCode()`

Returns a hash code for the current `Money` instance, based on the combination of `Amount` and `Currency`.

- **Returns**: A 32-bit signed integer hash code.

### `public override string ToString()`

Returns a string representation of the `Money` value, typically in the format `"{Amount} {Currency}"`.

- **Returns**: A string that represents the current `Money`.

### `public static bool operator <(Money left, Money right)`

Returns `true` if the amount of `left` is less than the amount of `right`, assuming the same currency.

- **Parameters**: `left`, `right` – `Money` operands.
- **Returns**: `true` if `left.Amount < right.Amount`; otherwise `false`.
- **Throws**: `InvalidOperationException` if the currencies of `left` and `right` differ.

### `public static bool operator >(Money left, Money right)`

Returns `true` if the amount of `left` is greater than the amount of `right`, assuming the same currency.

- **Parameters**: `left`, `right` – `Money` operands.
- **Returns**: `true` if `left.Amount > right.Amount`; otherwise `false`.
- **Throws**: `InvalidOperationException` if the currencies of `left` and `right` differ.

## Usage

### Example 1: Basic arithmetic and comparison

```csharp
var price = new Money(29.99m, "USD");
var tax = new Money(2.40m, "USD");

var total = price.Add(tax);
Console.WriteLine(total); // Output: 32.39 USD

var discount = new Money(5.00m, "USD");
var final = total.Subtract(discount);
Console.WriteLine(final); // Output: 27.39 USD

if (final.IsLessThan(price))
{
    Console.WriteLine("Final price is less than original.");
}
```

### Example 2: Equality and hashing

```csharp
var a = new Money(100.00m, "EUR");
var b = new Money(100.00m, "EUR");
var c = new Money(100.00m, "USD");

Console.WriteLine(a.Equals(b)); // True
Console.WriteLine(a == b);      // True (if == operator is defined, otherwise use Equals)
Console.WriteLine(a.Equals(c)); // False (different currency)

var set = new HashSet<Money> { a, b, c };
Console.WriteLine(set.Count); // 2 (a and b are equal, c is distinct)
```

## Notes

- **Currency mismatch**: All arithmetic and comparison methods throw `InvalidOperationException` when the `Currency` values of the operands do not match. Always ensure operands share the same currency before calling these members.
- **Mutable fields**: `Amount` and `Currency` are public fields and can be modified after construction. This breaks value-type semantics typically expected for monetary values. For safe concurrent access, avoid mutating these fields after the `Money` instance is shared across threads, or protect access with synchronization.
- **Thread safety**: The type is not inherently thread-safe. Concurrent reads of `Amount` and `Currency` are safe only if no writes occur. Methods that return new `Money` instances (e.g., `Add`, `Subtract`) are safe to call concurrently because they do not modify the original instance.
- **Negative amounts**: The type does not restrict `Amount` to non-negative values. Negative amounts are permitted and behave as expected in arithmetic and comparisons.
- **Null currency**: A `null` or empty `Currency` string is allowed but may cause unexpected behavior in equality checks and string formatting. Consider validating currency codes at the point of creation.
