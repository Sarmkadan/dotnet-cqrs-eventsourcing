#nullable enable
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
    /// <summary>Gets or sets the identifier of the account to retrieve.</summary>
    public string AccountId { get; set; }

    /// <summary>Gets or sets the correlation identifier used to trace the query.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the query was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>Initializes a new instance of the <see cref="GetAccountQuery"/> class.</summary>
    public GetAccountQuery()
    {
        AccountId = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>Initializes a new instance of the <see cref="GetAccountQuery"/> class for the specified account.</summary>
    /// <param name="accountId">The identifier of the account to retrieve.</param>
    public GetAccountQuery(string accountId)
        : this()
    {
        AccountId = accountId;
    }

    /// <summary>Returns a string representation of the query.</summary>
    /// <returns>A string containing the account identifier.</returns>
    public override string ToString()
        => $"GetAccountQuery {{ AccountId={AccountId} }}";
}

/// <summary>
/// Query to retrieve all accounts.
/// </summary>
public class GetAllAccountsQuery
{
    /// <summary>Gets or sets the page number to retrieve.</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets the number of accounts per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets or sets the correlation identifier used to trace the query.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the query was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>Initializes a new instance of the <see cref="GetAllAccountsQuery"/> class.</summary>
    public GetAllAccountsQuery()
    {
        PageNumber = 1;
        PageSize = 100;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>Initializes a new instance of the <see cref="GetAllAccountsQuery"/> class with the specified paging parameters.</summary>
    /// <param name="pageNumber">The page number to retrieve.</param>
    /// <param name="pageSize">The number of accounts per page.</param>
    public GetAllAccountsQuery(int pageNumber = 1, int pageSize = 100)
        : this()
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>Returns a string representation of the query.</summary>
    /// <returns>A string containing the paging parameters.</returns>
    public override string ToString()
        => $"GetAllAccountsQuery {{ Page={PageNumber}, Size={PageSize} }}";
}

/// <summary>
/// Query to get account transaction count.
/// </summary>
public sealed class GetTransactionCountQuery
{
    /// <summary>Gets or sets the identifier of the account whose transaction count is requested.</summary>
    public string AccountId { get; set; }

    /// <summary>Gets or sets the correlation identifier used to trace the query.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the query was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>Initializes a new instance of the <see cref="GetTransactionCountQuery"/> class.</summary>
    public GetTransactionCountQuery()
    {
        AccountId = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>Initializes a new instance of the <see cref="GetTransactionCountQuery"/> class for the specified account.</summary>
    /// <param name="accountId">The identifier of the account whose transaction count is requested.</param>
    public GetTransactionCountQuery(string accountId)
        : this()
    {
        AccountId = accountId;
    }

    /// <summary>Returns a string representation of the query.</summary>
    /// <returns>A string containing the account identifier.</returns>
    public override string ToString()
        => $"GetTransactionCountQuery {{ AccountId={AccountId} }}";
}

/// <summary>
/// Query to get account projection (read model).
/// </summary>
public sealed class GetAccountProjectionQuery
{
    /// <summary>Gets or sets the identifier of the account whose projection is requested.</summary>
    public string AccountId { get; set; }

    /// <summary>Gets or sets the correlation identifier used to trace the query.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the query was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>Initializes a new instance of the <see cref="GetAccountProjectionQuery"/> class.</summary>
    public GetAccountProjectionQuery()
    {
        AccountId = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>Initializes a new instance of the <see cref="GetAccountProjectionQuery"/> class for the specified account.</summary>
    /// <param name="accountId">The identifier of the account whose projection is requested.</param>
    public GetAccountProjectionQuery(string accountId)
        : this()
    {
        AccountId = accountId;
    }

    /// <summary>Returns a string representation of the query.</summary>
    /// <returns>A string containing the account identifier.</returns>
    public override string ToString()
        => $"GetAccountProjectionQuery {{ AccountId={AccountId} }}";
}

/// <summary>
/// Query to retrieve event stream for an account.
/// </summary>
public class GetEventStreamQuery
{
    /// <summary>Gets or sets the identifier of the aggregate whose event stream is requested.</summary>
    public string AggregateId { get; set; }

    /// <summary>Gets or sets the version from which to start reading the event stream.</summary>
    public long FromVersion { get; set; }

    /// <summary>Gets or sets the correlation identifier used to trace the query.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the query was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>Initializes a new instance of the <see cref="GetEventStreamQuery"/> class.</summary>
    public GetEventStreamQuery()
    {
        AggregateId = string.Empty;
        FromVersion = 0;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>Initializes a new instance of the <see cref="GetEventStreamQuery"/> class for the specified aggregate.</summary>
    /// <param name="aggregateId">The identifier of the aggregate whose event stream is requested.</param>
    /// <param name="fromVersion">The version from which to start reading the event stream.</param>
    public GetEventStreamQuery(string aggregateId, long fromVersion = 0)
        : this()
    {
        AggregateId = aggregateId;
        FromVersion = fromVersion;
    }

    /// <summary>Returns a string representation of the query.</summary>
    /// <returns>A string containing the aggregate identifier and starting version.</returns>
    public override string ToString()
        => $"GetEventStreamQuery {{ AggregateId={AggregateId}, FromVersion={FromVersion} }}";
}
