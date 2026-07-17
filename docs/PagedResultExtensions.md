# PagedResultExtensions

Provides LINQ-style extension methods for `PagedResult<T>` to enable filtering, projection, and collection conversion while preserving pagination metadata. These methods allow treating a paged result as a readable sequence without materializing the underlying data source.

## API

### `AsReadOnly<T>(this PagedResult<T> source)`
Returns a read-only view of the items in the current page.

**Parameters**  
- `source`: The paged result to wrap.

**Returns**  
`IReadOnlyList<T>` containing the page items.

**Throws**  
`ArgumentNullException` if `source` is null.

---

### `HasItems<T>(this PagedResult<T> source)`
Determines whether the current page contains any items.

**Parameters**  
- `source`: The paged result to check.

**Returns**  
`true` if the page has at least one item; otherwise `false`.

**Throws**  
`ArgumentNullException` if `source` is null.

---

### `IsEmpty<T>(this PagedResult<T> source)`
Determines whether the current page contains zero items.

**Parameters**  
- `source`: The paged result to check.

**Returns**  
`true` if the page has no items; otherwise `false`.

**Throws**  
`ArgumentNullException` if `source` is null.

---

### `AsSpan<T>(this PagedResult<T> source)`
Returns a read-only span over the items in the current page.

**Parameters**  
- `source`: The paged result to convert.

**Returns**  
`ReadOnlySpan<T>` backed by the page's item array.

**Throws**  
`ArgumentNullException` if `source` is null.

---

### `FirstOrDefault<T>(this PagedResult<T> source)`
Returns the first item in the current page, or `default` if the page is empty.

**Parameters**  
- `source`: The paged result to query.

**Returns**  
The first `T` item, or `null` (or default value type) if the page has no items.

**Throws**  
`ArgumentNullException` if `source` is null.

---

### `LastOrDefault<T>(this PagedResult<T> source)`
Returns the last item in the current page, or `default` if the page is empty.

**Parameters**  
- `source`: The paged result to query.

**Returns**  
The last `T` item, or `null` (or default value type) if the page has no items.

**Throws**  
`ArgumentNullException` if `source` is null.

---

### `Select<T, TResult>(this PagedResult<T> source, Func<T, TResult> selector)`
Projects each item in the current page to a new form, returning a new `PagedResult<TResult>` with identical pagination metadata.

**Parameters**  
- `source`: The paged result to project.
- `selector`: A transform function applied to each item.

**Returns**  
`PagedResult<TResult>` containing the projected items and original pagination info (page number, page size, total count).

**Throws**  
`ArgumentNullException` if `source` or `selector` is null.

---

### `Where<T>(this PagedResult<T> source, Func<T, bool> predicate)`
Filters the items in the current page, returning a new `PagedResult<T>` containing only matching items. Pagination metadata (page number, page size, total count) is preserved from the original result.

**Parameters**  
- `source`: The paged result to filter.
- `predicate`: A function to test each item for inclusion.

**Returns**  
`PagedResult<T>` with filtered items and original pagination metadata.

**Throws**  
`ArgumentNullException` if `source` or `predicate` is null.

---

### `ToArray<T>(this PagedResult<T> source)`
Copies the items in the current page to a new array.

**Parameters**  
- `source`: The paged result to convert.

**Returns**  
`T[]` containing the page items.

**Throws**  
`ArgumentNullException` if `source` is null.

---

### `ToList<T>(this PagedResult<T> source)`
Copies the items in the current page to a new `List<T>`.

**Parameters**  
- `source`: The paged result to convert.

**Returns**  
`List<T>` containing the page items.

**Throws**  
`ArgumentNullException` if `source` is null.

## Usage

```csharp
var pagedUsers = await userRepository.GetPagedAsync(page: 1, pageSize: 20);

if (pagedUsers.HasItems())
{
    var activeUsers = pagedUsers.Where(u => u.IsActive);
    var displayNames = activeUsers.Select(u => u.DisplayName).ToList();
    
    foreach (var name in displayNames)
    {
        Console.WriteLine(name);
    }
}
```

```csharp
var pagedOrders = await orderService.GetOrdersAsync(customerId, page: 2, pageSize: 50);

var firstOrder = pagedOrders.FirstOrDefault();
var lastOrder = pagedOrders.LastOrDefault();

if (firstOrder != null)
{
    var orderSummaries = pagedOrders
        .Where(o => o.Total > 100)
        .Select(o => new OrderSummary(o.Id, o.Total, o.CreatedAt))
        .ToArray();
    
    await ProcessSummariesAsync(orderSummaries);
}
```

## Notes

- All methods preserve the original `PagedResult` metadata (`PageNumber`, `PageSize`, `TotalCount`) except `Where`, which retains metadata but reduces the actual item count in the current page. Consumers should not rely on `Items.Count` matching `PageSize` after filtering.
- `Select` and `Where` return new `PagedResult` instances; the original is unmodified.
- `AsSpan` returns a span backed by the internal array. Do not use the span after the `PagedResult` instance is eligible for garbage collection if the underlying array is pooled or rented.
- None of the methods mutate the source `PagedResult`. The type is effectively immutable; thread safety depends on the immutability of `T` and the underlying collection implementation.
- `FirstOrDefault` and `LastOrDefault` return `default(T)` for empty pages. For value types, this is the zero-initialized value; for reference types, `null`. Use `HasItems` or `IsEmpty` to distinguish between a default value and an actual item when `T` is a value type.
