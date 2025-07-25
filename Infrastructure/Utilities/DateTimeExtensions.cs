#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// DateTime and TimeSpan extension methods for common date/time operations.
/// Includes UTC normalization, rounding, comparison, and formatting helpers.
/// All methods assume UTC timestamps for consistency across distributed systems.
/// </summary>
/// <remarks>
/// This class is sealed to prevent unnecessary inheritance and ensure consistent behavior.
/// </remarks>
public static class DateTimeExtensions
{
    /// <summary>
    /// Ensures a DateTime is in UTC. Throws if already specified as Local.
    /// Use this to catch timezone bugs early.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert to UTC.</param>
    /// <returns>The DateTime in UTC format.</returns>
    /// <exception cref="InvalidOperationException">Thrown when dateTime.Kind is DateTimeKind.Local.</exception>
    public static DateTime EnsureUtc(this DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Local)
        {
            throw new InvalidOperationException(
                "DateTime must be UTC. Convert using .ToUniversalTime() first."
            );
        }

        return dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
    }

    /// <summary>
    /// Rounds a DateTime to the nearest second, removing milliseconds.
    /// Useful for event timestamps and logging.
    /// </summary>
    /// <param name="dateTime">The DateTime to round.</param>
    /// <returns>A DateTime rounded to the nearest second.</returns>
    public static DateTime RoundToSeconds(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, DateTimeKind.Utc);
    }

    /// <summary>
    /// Rounds a DateTime to the nearest minute.
    /// </summary>
    /// <param name="dateTime">The DateTime to round.</param>
    /// <returns>A DateTime rounded to the nearest minute.</returns>
    public static DateTime RoundToMinutes(this DateTime dateTime)
    {
        return dateTime.AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);
    }

    /// <summary>
    /// Rounds a DateTime to the nearest hour.
    /// </summary>
    /// <param name="dateTime">The DateTime to round.</param>
    /// <returns>A DateTime rounded to the nearest hour.</returns>
    public static DateTime RoundToHours(this DateTime dateTime)
    {
        return dateTime.AddMinutes(-dateTime.Minute).AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);
    }

    /// <summary>
    /// Truncates a DateTime to a specific precision.
    /// Example: TruncateTo(TimeSpan.FromMinutes(1)) removes seconds/milliseconds.
    /// </summary>
    /// <param name="dateTime">The DateTime to truncate.</param>
    /// <param name="precision">The precision to truncate to.</param>
    /// <returns>A DateTime truncated to the specified precision.</returns>
    public static DateTime TruncateTo(this DateTime dateTime, TimeSpan precision)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % precision.Ticks));
    }

    /// <summary>
    /// Checks if a DateTime is in the past (before now).
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the DateTime is in the past; otherwise, false.</returns>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future (after now).
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the DateTime is in the future; otherwise, false.</returns>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is today (same calendar day in UTC).
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the DateTime is today; otherwise, false.</returns>
    public static bool IsToday(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        return dateTime.Date == now.Date;
    }

    /// <summary>
    /// Gets the age (difference) between a DateTime and now in years.
    /// Useful for calculating age from birthdate.
    /// </summary>
    /// <param name="dateTime">The DateTime to calculate age from.</param>
    /// <returns>The age in years.</returns>
    public static int AgeInYears(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var age = now.Year - dateTime.Year;

        if (dateTime.Date > now.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    /// <summary>
    /// Formats a DateTime in ISO 8601 format (e.g., "2026-05-04T12:34:56Z").
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>An ISO 8601 formatted string.</returns>
    public static string ToIso8601(this DateTime dateTime)
    {
        return dateTime.ToString("O");
    }

    /// <summary>
    /// Formats a DateTime in RFC 7231 format (HTTP headers).
    /// Example: "Sun, 04 May 2026 12:34:56 GMT"
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>An RFC 7231 formatted string.</returns>
    public static string ToRfc7231(this DateTime dateTime)
    {
        return dateTime.ToString("r");
    }

    /// <summary>
    /// Formats a DateTime in compact format (YYYYMMDD).
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>A compact date string.</returns>
    public static string ToCompactDate(this DateTime dateTime)
    {
        return dateTime.ToString("yyyyMMdd");
    }

    /// <summary>
    /// Gets start of day (00:00:00).
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of day for.</param>
    /// <returns>The start of the day.</returns>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets end of day (23:59:59).
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of day for.</param>
    /// <returns>The end of the day.</returns>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddSeconds(-1);
    }

    /// <summary>
    /// Gets start of month (1st day at 00:00:00).
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of month for.</param>
    /// <returns>The start of the month.</returns>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets end of month (last day at 23:59:59).
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of month for.</param>
    /// <returns>The end of the month.</returns>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        var firstDayNextMonth = dateTime.StartOfMonth().AddMonths(1);
        return firstDayNextMonth.AddSeconds(-1);
    }

    /// <summary>
    /// Gets start of year (January 1 at 00:00:00).
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of year for.</param>
    /// <returns>The start of the year.</returns>
    public static DateTime StartOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1);
    }

    /// <summary>
    /// Gets end of year (December 31 at 23:59:59).
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of year for.</param>
    /// <returns>The end of the year.</returns>
    public static DateTime EndOfYear(this DateTime dateTime)
    {
        return dateTime.StartOfYear().AddYears(1).AddSeconds(-1);
    }

    /// <summary>
    /// Gets the next occurrence of a specific day of week.
    /// Example: Monday.GetNextOccurrence(DayOfWeek.Friday) gets next Friday.
    /// </summary>
    /// <param name="dateTime">The DateTime to start from.</param>
    /// <param name="dayOfWeek">The target day of week to find.</param>
    /// <returns>The next occurrence of the specified day of week.</returns>
    public static DateTime GetNextOccurrenceOfDay(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        var daysAhead = dayOfWeek - dateTime.DayOfWeek;
        if (daysAhead <= 0)
        {
            daysAhead += 7;
        }

        return dateTime.AddDays(daysAhead);
    }

    /// <summary>
    /// Gets a human-readable time span (e.g., "2 hours ago", "in 3 minutes").
    /// </summary>
    /// <param name="dateTime">The DateTime to get relative time for.</param>
    /// <returns>A human-readable relative time string.</returns>
    public static string GetRelativeTime(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var timeSpan = now - dateTime;

        if (timeSpan.TotalMilliseconds < 1000)
        {
            return "just now";
        }

        if (timeSpan.TotalSeconds < 60)
        {
            return $"{(int)timeSpan.TotalSeconds} seconds ago";
        }

        if (timeSpan.TotalMinutes < 60)
        {
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        }

        if (timeSpan.TotalHours < 24)
        {
            return $"{(int)timeSpan.TotalHours} hours ago";
        }

        if (timeSpan.TotalDays < 7)
        {
            return $"{(int)timeSpan.TotalDays} days ago";
        }

        return dateTime.ToShortDateString();
    }
}

/// <summary>
/// TimeSpan extension methods for common operations.
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Gets a human-readable representation of a TimeSpan.
    /// Example: TimeSpan.FromHours(2.5) -> "2h 30m"
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to format.</param>
    /// <returns>A human-readable TimeSpan string.</returns>
    public static string ToHumanReadable(this TimeSpan timeSpan)
    {
        var parts = new List<string>();

        if (timeSpan.Days > 0)
        {
            parts.Add($"{timeSpan.Days}d");
        }

        if (timeSpan.Hours > 0)
        {
            parts.Add($"{timeSpan.Hours}h");
        }

        if (timeSpan.Minutes > 0)
        {
            parts.Add($"{timeSpan.Minutes}m");
        }

        if (timeSpan.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{timeSpan.Seconds}s");
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Checks if a TimeSpan is positive.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to check.</param>
    /// <returns>True if the TimeSpan is positive; otherwise, false.</returns>
    public static bool IsPositive(this TimeSpan timeSpan)
    {
        return timeSpan > TimeSpan.Zero;
    }

    /// <summary>
    /// Checks if a TimeSpan is negative.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to check.</param>
    /// <returns>True if the TimeSpan is negative; otherwise, false.</returns>
    public static bool IsNegative(this TimeSpan timeSpan)
    {
        return timeSpan < TimeSpan.Zero;
    }

    /// <summary>
    /// Doubles a TimeSpan.
    /// Example: TimeSpan.FromSeconds(5).Double() -> 10 seconds
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to double.</param>
    /// <returns>A new TimeSpan that is double the original.</returns>
    public static TimeSpan Double(this TimeSpan timeSpan)
    {
        return TimeSpan.FromMilliseconds(timeSpan.TotalMilliseconds * 2);
    }
}