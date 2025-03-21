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
public static class DateTimeExtensions
{
    /// <summary>
    /// Ensures a DateTime is in UTC. Throws if already specified as Local.
    /// Use this to catch timezone bugs early.
    /// </summary>
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
    public static DateTime RoundToSeconds(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
            dateTime.Hour, dateTime.Minute, dateTime.Second, DateTimeKind.Utc);
    }

    /// <summary>
    /// Rounds a DateTime to the nearest minute.
    /// </summary>
    public static DateTime RoundToMinutes(this DateTime dateTime)
    {
        return dateTime.AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);
    }

    /// <summary>
    /// Rounds a DateTime to the nearest hour.
    /// </summary>
    public static DateTime RoundToHours(this DateTime dateTime)
    {
        return dateTime.AddMinutes(-dateTime.Minute).AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);
    }

    /// <summary>
    /// Truncates a DateTime to a specific precision.
    /// Example: TruncateTo(TimeSpan.FromMinutes(1)) removes seconds/milliseconds.
    /// </summary>
    public static DateTime TruncateTo(this DateTime dateTime, TimeSpan precision)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % precision.Ticks));
    }

    /// <summary>
    /// Checks if a DateTime is in the past (before now).
    /// </summary>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future (after now).
    /// </summary>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is today (same calendar day in UTC).
    /// </summary>
    public static bool IsToday(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        return dateTime.Date == now.Date;
    }

    /// <summary>
    /// Gets the age (difference) between a DateTime and now in years.
    /// Useful for calculating age from birthdate.
    /// </summary>
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
    public static string ToIso8601(this DateTime dateTime)
    {
        return dateTime.ToString("O");
    }

    /// <summary>
    /// Formats a DateTime in RFC 7231 format (HTTP headers).
    /// Example: "Sun, 04 May 2026 12:34:56 GMT"
    /// </summary>
    public static string ToRfc7231(this DateTime dateTime)
    {
        return dateTime.ToString("r");
    }

    /// <summary>
    /// Formats a DateTime in compact format (YYYYMMDD).
    /// </summary>
    public static string ToCompactDate(this DateTime dateTime)
    {
        return dateTime.ToString("yyyyMMdd");
    }

    /// <summary>
    /// Gets start of day (00:00:00).
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets end of day (23:59:59).
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddSeconds(-1);
    }

    /// <summary>
    /// Gets start of month (1st day at 00:00:00).
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets end of month (last day at 23:59:59).
    /// </summary>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        var firstDayNextMonth = dateTime.StartOfMonth().AddMonths(1);
        return firstDayNextMonth.AddSeconds(-1);
    }

    /// <summary>
    /// Gets start of year (January 1 at 00:00:00).
    /// </summary>
    public static DateTime StartOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1);
    }

    /// <summary>
    /// Gets end of year (December 31 at 23:59:59).
    /// </summary>
    public static DateTime EndOfYear(this DateTime dateTime)
    {
        return dateTime.StartOfYear().AddYears(1).AddSeconds(-1);
    }

    /// <summary>
    /// Gets the next occurrence of a specific day of week.
    /// Example: Monday.GetNextOccurrence(DayOfWeek.Friday) gets next Friday.
    /// </summary>
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
    public static bool IsPositive(this TimeSpan timeSpan)
    {
        return timeSpan > TimeSpan.Zero;
    }

    /// <summary>
    /// Checks if a TimeSpan is negative.
    /// </summary>
    public static bool IsNegative(this TimeSpan timeSpan)
    {
        return timeSpan < TimeSpan.Zero;
    }

    /// <summary>
    /// Doubles a TimeSpan.
    /// Example: TimeSpan.FromSeconds(5).Double() -> 10 seconds
    /// </summary>
    public static TimeSpan Double(this TimeSpan timeSpan)
    {
        return TimeSpan.FromMilliseconds(timeSpan.TotalMilliseconds * 2);
    }
}
