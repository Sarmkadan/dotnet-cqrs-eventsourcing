# ICsvFormatter

`ICsvFormatter` defines a contract for converting collections of objects into CSV-formatted strings. Implementations of this interface provide configurable options for column selection, header inclusion, delimiter choice, and date formatting to support consistent CSV serialization across different contexts.

## API

### `string Format<T>(IEnumerable<T> items)`
Serializes the given sequence of objects into a CSV-formatted string.

- **Parameters**
  - `items`: The collection of objects to serialize. Must not be null.
- **Returns**
  - A string containing the CSV representation of `items`.
- **Throws**
  - `ArgumentNullException`: If `items` is null.

### `string FormatWithoutHeaders<T>(IEnumerable<T> items)`
Serializes the given sequence of objects into a CSV-formatted string without including headers.

- **Parameters**
  - `items`: The collection of objects to serialize. Must not be null.
- **Returns**
  - A string containing the CSV representation of `items` without headers.
- **Throws**
  - `ArgumentNullException`: If `items` is null.

### `IEnumerable<string> GetColumns<T>()`
Retrieves the names of the columns that will be included in the CSV output for type `T`.

- **Type Parameters**
  - `T`: The type of object for which to retrieve columns.
- **Returns**
  - An enumerable of column names in the order they will appear in the CSV output.
- **Throws**
  - `ArgumentException`: If no columns are defined for `T` via `CsvColumnAttribute`.

### `char Delimiter`
Gets the character used to separate values in the CSV output.

- **Type**
  - `char`
- **Remarks**
  - Defaults to a comma (`,`) unless overridden via `CsvFormatOptions`.

### `bool IncludeHeaders`
Gets a value indicating whether headers should be included in the CSV output.

- **Type**
  - `bool`
- **Remarks**
  - Defaults to `true`.

### `string DateFormat`
Gets the format string used when converting `DateTime` or `DateTimeOffset` values to strings.

- **Type**
  - `string`
- **Remarks**
  - Defaults to `"yyyy-MM-dd HH:mm:ss"` unless specified otherwise.

### `public static CsvFormatOptions Default`
Provides default formatting options for CSV serialization.

- **Type**
  - `CsvFormatOptions`
- **Remarks**
  - Uses comma delimiter, includes headers, and applies the default date format.

### `public static CsvFormatOptions WithSemicolonDelimiter`
Provides formatting options configured to use a semicolon (`;`) as the delimiter.

- **Type**
  - `CsvFormatOptions`
- **Remarks**
  - Includes headers and uses the default date format.

### `public static CsvFormatOptions WithTabDelimiter`
Provides formatting options configured to use a tab (`\t`) as the delimiter.

- **Type**
  - `CsvFormatOptions`
- **Remarks**
  - Includes headers and uses the default date format.

### `string Name`
Gets the name of the formatter instance.

- **Type**
  - `string`
- **Remarks**
  - Used for identification in logging or multi-formatter scenarios.

### `int Order`
Gets the priority order of the formatter when multiple formatters are registered.

- **Type**
  - `int`
- **Remarks**
  - Lower values indicate higher priority.

### `public CsvColumnAttribute`
Attribute used to decorate properties on a type to indicate they should be included as columns in CSV output.

- **Remarks**
  - Applied to properties of types serialized by `ICsvFormatter`.

### `public static IServiceCollection AddCsvFormatter(IServiceCollection services)`
Registers the default CSV formatter implementation with the dependency injection container.

- **Parameters**
  - `services`: The `IServiceCollection` to which the formatter is added.
- **Returns**
  - The same `IServiceCollection` for method chaining.
- **Remarks**
  - Registers `ICsvFormatter` as a singleton with default options unless overridden.

## Usage

### Basic CSV Serialization
