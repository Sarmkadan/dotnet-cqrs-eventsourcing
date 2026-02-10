// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Commands;

/// <summary>
/// Command to create a new account in the system.
/// </summary>
public class CreateAccountCommand
{
    public string AccountNumber { get; set; }
    public string AccountHolder { get; set; }
    public string Currency { get; set; }
    public decimal InitialBalance { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public CreateAccountCommand()
    {
        AccountNumber = string.Empty;
        AccountHolder = string.Empty;
        Currency = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public CreateAccountCommand(string accountNumber, string accountHolder, string currency, decimal initialBalance)
        : this()
    {
        AccountNumber = accountNumber;
        AccountHolder = accountHolder;
        Currency = currency;
        InitialBalance = initialBalance;
    }

    public override string ToString()
        => $"CreateAccountCommand {{ AccountNumber={AccountNumber}, Holder={AccountHolder}, Currency={Currency}, Balance={InitialBalance} }}";
}

/// <summary>
/// Command to deposit funds into an account.
/// </summary>
public class DepositCommand
{
    public string AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public DepositCommand()
    {
        AccountId = string.Empty;
        Reference = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public DepositCommand(string accountId, decimal amount, string reference)
        : this()
    {
        AccountId = accountId;
        Amount = amount;
        Reference = reference;
    }

    public override string ToString()
        => $"DepositCommand {{ AccountId={AccountId}, Amount={Amount}, Reference={Reference} }}";
}

/// <summary>
/// Command to withdraw funds from an account.
/// </summary>
public class WithdrawCommand
{
    public string AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public WithdrawCommand()
    {
        AccountId = string.Empty;
        Reference = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public WithdrawCommand(string accountId, decimal amount, string reference)
        : this()
    {
        AccountId = accountId;
        Amount = amount;
        Reference = reference;
    }

    public override string ToString()
        => $"WithdrawCommand {{ AccountId={AccountId}, Amount={Amount}, Reference={Reference} }}";
}

/// <summary>
/// Command to close an account.
/// </summary>
public class CloseAccountCommand
{
    public string AccountId { get; set; }
    public string Reason { get; set; }
    public string CorrelationId { get; set; }
    public DateTime IssuedAt { get; set; }

    public CloseAccountCommand()
    {
        AccountId = string.Empty;
        Reason = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    public CloseAccountCommand(string accountId, string reason)
        : this()
    {
        AccountId = accountId;
        Reason = reason;
    }

    public override string ToString()
        => $"CloseAccountCommand {{ AccountId={AccountId}, Reason={Reason} }}";
}
