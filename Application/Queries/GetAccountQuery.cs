// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Queries;

/// <summary>
/// Query to retrieve an account by ID.
/// </summary>
public class GetAccountQuery
{
    public string AccountId { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public GetAccountQuery()
    {
        AccountId = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public GetAccountQuery(string accountId)
        : this()
    {
        AccountId = accountId;
    }

    public override string ToString()
        => $"GetAccountQuery {{ AccountId={AccountId} }}";
}

/// <summary>
/// Query to retrieve all accounts.
/// </summary>
public class GetAllAccountsQuery
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public GetAllAccountsQuery()
    {
        PageNumber = 1;
        PageSize = 100;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public GetAllAccountsQuery(int pageNumber = 1, int pageSize = 100)
        : this()
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public override string ToString()
        => $"GetAllAccountsQuery {{ Page={PageNumber}, Size={PageSize} }}";
}

/// <summary>
/// Query to get account transaction count.
/// </summary>
public class GetTransactionCountQuery
{
    public string AccountId { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public GetTransactionCountQuery()
    {
        AccountId = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public GetTransactionCountQuery(string accountId)
        : this()
    {
        AccountId = accountId;
    }

    public override string ToString()
        => $"GetTransactionCountQuery {{ AccountId={AccountId} }}";
}

/// <summary>
/// Query to get account projection (read model).
/// </summary>
public class GetAccountProjectionQuery
{
    public string AccountId { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public GetAccountProjectionQuery()
    {
        AccountId = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public GetAccountProjectionQuery(string accountId)
        : this()
    {
        AccountId = accountId;
    }

    public override string ToString()
        => $"GetAccountProjectionQuery {{ AccountId={AccountId} }}";
}

/// <summary>
/// Query to retrieve event stream for an account.
/// </summary>
public class GetEventStreamQuery
{
    public string AggregateId { get; set; }
    public long FromVersion { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public GetEventStreamQuery()
    {
        AggregateId = string.Empty;
        FromVersion = 0;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public GetEventStreamQuery(string aggregateId, long fromVersion = 0)
        : this()
    {
        AggregateId = aggregateId;
        FromVersion = fromVersion;
    }

    public override string ToString()
        => $"GetEventStreamQuery {{ AggregateId={AggregateId}, FromVersion={FromVersion} }}";
}
