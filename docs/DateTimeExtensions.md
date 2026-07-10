# DateTimeExtensions

A utility class providing common date and time manipulation and formatting operations for `System.DateTime`. These extensions simplify tasks such as rounding timestamps, determining temporal relationships, calculating ages, and generating standardized date strings in various formats.

## API

### `public static DateTime EnsureUtc(DateTime dateTime)`

Ensures the given `DateTime` instance is in UTC by converting it if necessary. If the input is `DateTimeKind.Unspecified`, it is assumed to be UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to convert to UTC.
- **Returns**
  - DateTime: A `DateTime` instance with `Kind` set to `DateTimeKind.Utc`.
- **Throws**
  - `ArgumentException`: If the input `Kind` is `DateTimeKind.Local` and the system does not support local time zone conversion.

---

### `public static DateTime RoundToSeconds(DateTime dateTime)`

Rounds the given `DateTime` down to the nearest whole second.

- **Parameters**
  - `dateTime` (DateTime): The date and time to round.
- **Returns**
  - DateTime: A new `DateTime` with milliseconds and smaller units truncated.

---

### `public static DateTime RoundToMinutes(DateTime dateTime)`

Rounds the given `DateTime` down to the nearest whole minute.

- **Parameters**
  - `dateTime` (DateTime): The date and time to round.
- **Returns**
  - DateTime: A new `DateTime` with seconds, milliseconds, and smaller units truncated.

---

### `public static DateTime RoundToHours(DateTime dateTime)`

Rounds the given `DateTime` down to the nearest whole hour.

- **Parameters**
  - `dateTime` (DateTime): The date and time to round.
- **Returns**
  - DateTime: A new `DateTime` with minutes, seconds, milliseconds, and smaller units truncated.

---

### `public static DateTime TruncateTo(DateTime dateTime, TimeSpan precision)`

Truncates the given `DateTime` to the specified precision.

- **Parameters**
  - `dateTime` (DateTime): The date and time to truncate.
  - `precision` (TimeSpan): The smallest unit to retain (e.g., `TimeSpan.FromSeconds(1)` for seconds).
- **Returns**
  - DateTime: A new `DateTime` truncated to the given precision.
- **Throws**
  - `ArgumentOutOfRangeException`: If `precision` is zero or negative, or not a power-of-two fraction of a day.

---

### `public static bool IsPast(DateTime dateTime)`

Determines whether the given `DateTime` is in the past relative to `DateTime.UtcNow`.

- **Parameters**
  - `dateTime` (DateTime): The date and time to evaluate.
- **Returns**
  - bool: `true` if the date is strictly before the current UTC time; otherwise, `false`.

---

### `public static bool IsFuture(DateTime dateTime)`

Determines whether the given `DateTime` is in the future relative to `DateTime.UtcNow`.

- **Parameters**
  - `dateTime` (DateTime): The date and time to evaluate.
- **Returns**
  - bool: `true` if the date is strictly after the current UTC time; otherwise, `false`.

---

### `public static bool IsToday(DateTime dateTime)`

Determines whether the given `DateTime` falls on the current UTC date.

- **Parameters**
  - `dateTime` (DateTime): The date and time to evaluate.
- **Returns**
  - bool: `true` if the date part matches today’s UTC date; otherwise, `false`.

---

### `public static int AgeInYears(DateTime birthDate)`

Calculates the age in whole years from the given birth date to the current UTC date.

- **Parameters**
  - `birthDate` (DateTime): The birth date to calculate age from.
- **Returns**
  - int: The number of whole years elapsed since `birthDate`.
- **Throws**
  - `ArgumentOutOfRangeException`: If `birthDate` is in the future.

---

### `public static string ToIso8601(DateTime dateTime)`

Formats the given `DateTime` as an ISO 8601 string in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to format.
- **Returns**
  - string: An ISO 8601 compliant UTC timestamp (e.g., `"2024-05-20T14:30:00Z"`).

---

### `public static string ToRfc7231(DateTime dateTime)`

Formats the given `DateTime` as an RFC 7231 formatted HTTP-date string in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to format.
- **Returns**
  - string: A string compliant with RFC 7231 (e.g., `"Tue, 20 May 2024 14:30:00 GMT"`).

---

### `public static string ToCompactDate(DateTime dateTime)`

Formats the given `DateTime` as a compact date string in ISO-like format without separators.

- **Parameters**
  - `dateTime` (DateTime): The date and time to format.
- **Returns**
  - string: A compact date-time string (e.g., `"20240520143000"`).

---

### `public static DateTime StartOfDay(DateTime dateTime)`

Returns a `DateTime` representing the start of the day (00:00:00) for the given date in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to derive the start of day from.
- **Returns**
  - DateTime: A new `DateTime` at midnight UTC on the same calendar day.

---

### `public static DateTime EndOfDay(DateTime dateTime)`

Returns a `DateTime` representing the end of the day (23:59:59.999) for the given date in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to derive the end of day from.
- **Returns**
  - DateTime: A new `DateTime` at one tick before midnight UTC on the following day.

---

### `public static DateTime StartOfMonth(DateTime dateTime)`

Returns a `DateTime` representing the first moment of the month for the given date in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to derive the start of month from.
- **Returns**
  - DateTime: A new `DateTime` at midnight UTC on the first day of the same month and year.

---

### `public static DateTime EndOfMonth(DateTime dateTime)`

Returns a `DateTime` representing the last moment of the month for the given date in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to derive the end of month from.
- **Returns**
  - DateTime: A new `DateTime` at one tick before midnight UTC on the first day of the following month.

---

### `public static DateTime StartOfYear(DateTime dateTime)`

Returns a `DateTime` representing the start of the year for the given date in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to derive the start of year from.
- **Returns**
  - DateTime: A new `DateTime` at midnight UTC on January 1st of the same year.

---

### `public static DateTime EndOfYear(DateTime dateTime)`

Returns a `DateTime` representing the end of the year for the given date in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to derive the end of year from.
- **Returns**
  - DateTime: A new `DateTime` at one tick before midnight UTC on January 1st of the following year.

---

### `public static DateTime GetNextOccurrenceOfDay(DateTime dateTime, DayOfWeek dayOfWeek)`

Returns the next occurrence of the specified day of the week after the given date in UTC.

- **Parameters**
  - `dateTime` (DateTime): The starting date and time.
  - `dayOfWeek` (DayOfWeek): The target day of the week.
- **Returns**
  - DateTime: A `DateTime` on the next occurrence of `dayOfWeek` at the same time of day.
- **Throws**
  - `ArgumentOutOfRangeException`: If `dayOfWeek` is not a valid `DayOfWeek` value.

---
### `public static string GetRelativeTime(DateTime dateTime)`

Generates a human-readable relative time string (e.g., "2 minutes ago") for the given date in UTC.

- **Parameters**
  - `dateTime` (DateTime): The date and time to describe.
- **Returns**
  - string: A localized relative time phrase (e.g., `"in 5 hours"`, `"3 days ago"`).

## Usage
