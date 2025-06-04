using System;
using System.Collections.Generic;

namespace DotNetCqrsEventSourcing.Infrastructure.Models;

public static class RequestLogValidation
{
    public static IReadOnlyList<string> Validate(this RequestLog value)
    {
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

        if (string.IsNullOrWhiteSpace(value.Path))
        {
            problems.Add("Path cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(value.ClientIp))
        {
            problems.Add("ClientIp cannot be null or empty.");
        }

        return problems;
    }

    public static bool IsValid(this RequestLog value)
    {
        return value.Validate().Count == 0;
    }

    public static void EnsureValid(this RequestLog value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException($"RequestLog is invalid: {string.Join(", ", problems)}");
        }
    }
}
