# StringExtensions

Utility class providing common string manipulation and validation extensions for C# applications, particularly within the `dotnet-cqrs-eventsourcing` domain.

## API

### `ToSlug(string input)`
Converts a string into a URL-friendly slug by replacing spaces and special characters with hyphens, converting to lowercase, and removing leading/trailing hyphens. Non-alphanumeric characters are replaced with hyphens.

- **Parameters**: `input` – The string to convert.
- **Return value**: The slugified string.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---

### `ToCamelCase(string input)`
Converts a string to camelCase by lowercasing the first character and preserving the rest of the casing.

- **Parameters**: `input` – The string to convert.
- **Return value**: The camelCased string.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---

### `ToPascalCase(string input)`
Converts a string to PascalCase by capitalizing the first character and preserving the rest of the casing.

- **Parameters**: `input` – The string to convert.
- **Return value**: The PascalCased string.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---
### `ToSnakeCase(string input)`
Converts a string to snake_case by inserting underscores before uppercase letters (except the first), converting to lowercase, and collapsing multiple underscores.

- **Parameters**: `input` – The string to convert.
- **Return value**: The snake_cased string.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---
### `Truncate(string input, int maxLength, string suffix = "...")`
Truncates a string to a specified maximum length, appending an optional suffix if truncation occurs.

- **Parameters**:
  - `input` – The string to truncate.
  - `maxLength` – The maximum allowed length of the result.
  - `suffix` – Optional suffix to append when truncating. Defaults to `"..."`.
- **Return value**: The truncated string, or the original string if it is shorter than `maxLength`.
- **Throws**:
  - `ArgumentNullException` if `input` is `null`.
  - `ArgumentOutOfRangeException` if `maxLength` is negative.

---
### `Repeat(string input, int count)`
Repeats the input string a specified number of times.

- **Parameters**:
  - `input` – The string to repeat.
  - `count` – The number of repetitions.
- **Return value**: The repeated string.
- **Throws**:
  - `ArgumentNullException` if `input` is `null`.
  - `ArgumentOutOfRangeException` if `count` is negative.

---
### `IsValidEmail(string input)`
Validates whether the input string is a syntactically valid email address.

- **Parameters**: `input` – The string to validate.
- **Return value**: `true` if the string is a valid email; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---
### `IsValidGuid(string input)`
Validates whether the input string is a valid GUID (case-insensitive).

- **Parameters**: `input` – The string to validate.
- **Return value**: `true` if the string is a valid GUID; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---
### `RemoveWhitespace(string input)`
Removes all whitespace characters from the input string.

- **Parameters**: `input` – The string to process.
- **Return value**: The whitespace-free string.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---
### `EnsureStartsWith(string input, string prefix)`
Ensures the input string begins with the specified prefix. If it already starts with the prefix, returns the original string; otherwise, prepends the prefix.

- **Parameters**:
  - `input` – The string to check.
  - `prefix` – The required prefix.
- **Return value**: The string starting with `prefix`.
- **Throws**:
  - `ArgumentNullException` if `input` or `prefix` is `null`.

---
### `EnsureEndsWith(string input, string suffix)`
Ensures the input string ends with the specified suffix. If it already ends with the suffix, returns the original string; otherwise, appends the suffix.

- **Parameters**:
  - `input` – The string to check.
  - `suffix` – The required suffix.
- **Return value**: The string ending with `suffix`.
- **Throws**:
  - `ArgumentNullException` if `input` or `suffix` is `null`.

---
### `AlphanumericOnly(string input)`
Removes all non-alphanumeric characters from the input string.

- **Parameters**: `input` – The string to process.
- **Return value**: The alphanumeric-only string.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---
### `IsNumeric(string input)`
Determines whether the input string represents a numeric value (integer or decimal).

- **Parameters**: `input` – The string to evaluate.
- **Return value**: `true` if the string is numeric; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `input` is `null`.

---
### `PadLeft(string input, int totalWidth, char paddingChar = ' ')`
Left-pads the string with the specified padding character to ensure a minimum total width.

- **Parameters**:
  - `input` – The string to pad.
  - `totalWidth` – The minimum width of the resulting string.
  - `paddingChar` – The character to use for padding. Defaults to space.
- **Return value**: The padded string.
- **Throws**:
  - `ArgumentNullException` if `input` is `null`.
  - `ArgumentOutOfRangeException` if `totalWidth` is negative.

## Usage
