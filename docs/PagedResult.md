# PagedResult

`PagedResult<T>` is a generic container that represents a single page of data retrieved from a larger dataset. It bundles the items for the current page together with pagination metadata—page number, page size, and total record count—so that consumers can render paginated views without needing separate count queries or manual offset calculations. The type also exposes static factory and helper methods that centralize pagination logic, parameter validation, and conversion to API-friendly response shapes.

## API

### Properties

- **`List<T> Items`**  
  The list of elements belonging to the current page. When the requested page lies beyond the available data, this list is empty.

- **`int PageNumber`**  
  The one-based index of the current page. Always a positive integer after construction through the provided static methods.

- **`int PageSize`**  
  The maximum number of items that can appear on a single page. Must be greater than zero when set through the validation or factory methods.

- **`long TotalCount`**  
  The total number of records across all pages. Used by consumers to compute total pages and determine whether additional pages exist.

### Static Methods

- **`static PagedResult<T> Paginate<T>(List<T> items, long totalCount, int pageNumber, int pageSize)`**  
  Creates a `PagedResult<T>` directly from an already-fetched page of items and the known total count.  
  *Parameters:*  
  - `items` — the subset of data for the requested page.  
  - `totalCount` — total records in the full dataset.  
  - `pageNumber` — one-based page index.  
  - `pageSize` — maximum items per page.  
  *Returns:* a new `PagedResult<T>` with the supplied values.  
  *Throws:* typically throws `ArgumentOutOfRangeException` if `pageNumber` or `pageSize` is less than 1, or if `totalCount` is negative.

- **`static PagedResult<T> PaginateQuery<T>(IQueryable<T> query, int pageNumber, int pageSize)`**  
  Executes the provided `IQueryable<T>` against the data source, captures the total count via `LongCount()` (or equivalent), and materialises only the requested page.  
  *Parameters:*  
  - `query` — an `IQueryable<T>` representing the unfiltered, unordered dataset (ordering should be applied before calling).  
  - `pageNumber` — one-based page index.  
  - `pageSize` — maximum items per page.  
  *Returns:* a `PagedResult<T>` where `Items` contains the fetched page and `TotalCount` reflects the full dataset size.  
  *Throws:* `ArgumentNullException` when `query` is null; `ArgumentOutOfRangeException` for invalid `pageNumber` or `pageSize`; may propagate provider-specific exceptions from the underlying `IQueryable` execution.

- **`static (int pageNumber, int pageSize) ValidatePaginationParams(int pageNumber, int pageSize, int maxPageSize = 100)`**  
  Normalises and validates raw pagination inputs, clamping or rejecting values that fall outside acceptable bounds.  
  *Parameters:*  
  - `pageNumber` — requested page; values less than 1 are coerced to 1.  
  - `pageSize` — requested page size; values less than 1 are coerced to the default (often 10), values exceeding `maxPageSize` are clamped to `maxPageSize`.  
  - `maxPageSize` — optional ceiling for `pageSize` (default 100).  
  *Returns:* a tuple with sanitised `pageNumber` and `pageSize`.  
  *Throws:* does not throw; always returns usable values.

- **`static (int offset, int limit) GetOffsetAndLimit(int pageNumber, int pageSize)`**  
  Converts one-based pagination parameters into zero-based offset and limit values suitable for `Skip`/`Take` or raw SQL.  
  *Parameters:*  
  - `pageNumber` — one-based page index.  
  - `pageSize` — number of records per page.  
  *Returns:* a tuple where `offset = (pageNumber - 1) * pageSize` and `limit = pageSize`.  
  *Throws:* `ArgumentOutOfRangeException` when `pageNumber` or `pageSize` is less than 1.

- **`static PagedResult<T> ToPagedResult<T>(List<T> items, long totalCount, int pageNumber, int pageSize)`**  
  Overload of `Paginate<T>` with identical behaviour. Provided for scenarios where the name `ToPagedResult` better expresses intent (e.g. mapping from an internal DTO).  
  *Parameters / Returns / Throws:* same as `Paginate<T>`.

- **`static PagedResult<T> ToPagedResult<T>(IEnumerable<T> items, long totalCount, int pageNumber, int pageSize)`**  
  Overload accepting an `IEnumerable<T>` instead of `List<T>`. Internally materialises the enumerable into a list via `ToList()` before constructing the result.  
  *Parameters:*  
  - `items` — the page data as any `IEnumerable<T>`.  
  - `totalCount`, `pageNumber`, `pageSize` — as above.  
  *Returns:* a new `PagedResult<T>`.  
  *Throws:* `ArgumentNullException` when `items` is null; `ArgumentOutOfRangeException` for invalid numeric parameters.

- **`static object ToApiResponse<T>(PagedResult<T> result)`**  
  Projects a `PagedResult<T>` into an anonymous object (or a dedicated response DTO) suitable for JSON serialisation in web APIs. The returned object typically contains properties such as `items`, `pageNumber`, `pageSize`, `totalCount`, and possibly computed values like `totalPages`.  
  *Parameters:*  
  - `result` — a populated `PagedResult<T>`.  
  *Returns:* an `object` that serialises to a standard paginated response envelope.  
  *Throws:* `ArgumentNullException` when `result` is null.

## Usage

### Example 1: Manual construction from an in-memory list

```csharp
var allRecords = Enumerable.Range(1, 250).ToList();
int pageNumber = 3;
int pageSize = 20;

var (validPage, validSize) = PagedResult<int>.ValidatePaginationParams(pageNumber, pageSize);
var (offset, limit) = PagedResult<int>.GetOffsetAndLimit(validPage, validSize);

var pageItems = allRecords.Skip(offset).Take(limit).ToList();
var paged = PagedResult<int>.ToPagedResult(pageItems, allRecords.Count, validPage, validSize);

// Serialise for an API response
object apiResponse = PagedResult<int>.ToApiResponse(paged);
```

### Example 2: Database pagination with Entity Framework

```csharp
using var context = new AppDbContext();
IQueryable<Customer> baseQuery = context.Customers
    .Where(c => c.IsActive)
    .OrderBy(c => c.LastName);

int requestedPage = 2;
int requestedSize = 50;

var (pageNumber, pageSize) = PagedResult<Customer>.ValidatePaginationParams(requestedPage, requestedSize);
PagedResult<Customer> result = PagedResult<Customer>.PaginateQuery(baseQuery, pageNumber, pageSize);

Console.WriteLine($"Showing page {result.PageNumber} of {Math.Ceiling((double)result.TotalCount / result.PageSize)}");
foreach (var customer in result.Items)
{
    Console.WriteLine(customer.LastName);
}
```

## Notes

- **Empty pages:** When `PageNumber` exceeds the total number of pages, `Items` is an empty list, `TotalCount` remains the true total, and `PageNumber` retains the requested value. Consumers should compute `totalPages` from `TotalCount` and `PageSize` to detect this condition.
- **Validation boundaries:** `ValidatePaginationParams` never throws; it silently coerces `pageNumber` to a minimum of 1 and `pageSize` to a default minimum (commonly 10) while capping it at `maxPageSize`. Callers that need strict rejection should perform their own guard checks before calling the method.
- **Thread safety:** `PagedResult<T>` is an immutable data holder once constructed. Its static methods do not mutate shared state and are safe to call concurrently. The `PaginateQuery<T>` method relies on the thread safety guarantees of the underlying `IQueryable` provider; it does not introduce additional synchronisation.
- **`ToApiResponse` shape:** The exact shape of the returned object is an implementation detail and may include computed fields such as `totalPages`, `hasNextPage`, and `hasPreviousPage`. Serialisation behaviour depends on the serializer in use (e.g. `System.Text.Json` or Newtonsoft.Json). The method is designed for web API boundaries and should not be used for internal domain logic.
- **`IQueryable` enumeration:** `PaginateQuery<T>` executes the query twice—once for `LongCount()` and once for the paged `Skip/Take`. When using providers that do not efficiently support multiple enumerations (e.g. certain in-memory or stream-based sources), consider materialising the data first and using the `Paginate<T>` overload instead.
