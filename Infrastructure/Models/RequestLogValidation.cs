using System;
using System.Collections.Generic;

namespace DotNetCqrsEventSourcing.Infrastructure.Models;

/// <summary>
/// Provides validation methods for <see cref="RequestLog"/> instances.
/// Validates that required fields are present and properly formatted.
/// </summary>
public static class RequestLogValidation
{
    /// <summary>
    /// Validates the specified <see cref="RequestLog"/> instance.
    /// </summary>
    /// <param name="value">The request log to validate. Cannot be null.</param>
    /// <returns>A list of validation error messages. Empty if validation succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this RequestLog? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(value.RequestId))
        {
            problems.Add("RequestId cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(value.CorrelationId))
        {
            problems.Add("CorrelationId cannot be null or empty.");
        }

        if (value.Timestamp == default)
        {
            problems.Add("Timestamp cannot be the default date.");
        }

        if (string.IsNullOrWhiteSpace(value.Method))
        {
            problems.Add("Method cannot be null or empty.");
        }
        else if (!IsValidHttpMethod(value.Method))
        {
            problems.Add("Method must be a valid HTTP method (e.g., GET, POST, PUT, DELETE).");
        }

        if (string.IsNullOrWhiteSpace(value.Path))
        {
            problems.Add("Path cannot be null or empty.");
        }
        else if (!value.Path.StartsWith('/'))
        {
            problems.Add("Path must start with '/'.");
        }

        if (string.IsNullOrWhiteSpace(value.ClientIp))
        {
            problems.Add("ClientIp cannot be null or empty.");
        }
        else if (!IsValidIpAddress(value.ClientIp))
        {
            problems.Add("ClientIp must be a valid IP address.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="RequestLog"/> is valid.
    /// </summary>
    /// <param name="value">The request log to check. Cannot be null.</param>
    /// <returns>True if the request log is valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this RequestLog? value) => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="RequestLog"/> is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The request log to validate. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the request log is invalid.</exception>
    public static void EnsureValid(this RequestLog? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException($"RequestLog is invalid: {string.Join(", ", problems)}", nameof(value));
        }
    }

    /// <summary>
    /// Checks if the specified string is a valid HTTP method.
    /// </summary>
    /// <param name="method">The HTTP method to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    private static bool IsValidHttpMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return false;
        }

        return method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the specified string is a valid IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if valid; otherwise false.</returns>
    private static bool IsValidIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        // Basic validation - check for common IP patterns
        // IPv4: 192.168.1.1
        // IPv6: 2001:0db8:85a3:0000:0000:8a2e:0370:7334 or ::1
        return ipAddress.Split('.').Length == 4 ||
               ipAddress.Contains(':') && ipAddress.Length >= 2;
    }
}
