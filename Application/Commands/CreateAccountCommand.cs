#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Commands;

/// <summary>
/// Command to create a new account in the system.
/// </summary>
public sealed class CreateAccountCommand
{
    /// <summary>Gets or sets the account number.</summary>
    public string AccountNumber { get; set; }

    /// <summary>Gets or sets the name of the account holder.</summary>
    public string AccountHolder { get; set; }

    /// <summary>Gets or sets the ISO currency code for the account.</summary>
    public string Currency { get; set; }

    /// <summary>Gets or sets the initial balance of the account.</summary>
    public decimal InitialBalance { get; set; }

    /// <summary>Gets or sets the correlation id used to trace the command.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the command was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAccountCommand"/> class
    /// with default values and a fresh correlation id.
    /// </summary>
    public CreateAccountCommand()
    {
        AccountNumber = string.Empty;
        AccountHolder = string.Empty;
        Currency = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAccountCommand"/> class
    /// with the specified account details.
    /// </summary>
    /// <param name="accountNumber">The account number.</param>
    /// <param name="accountHolder">The name of the account holder.</param>
    /// <param name="currency">The ISO currency code for the account.</param>
    /// <param name="initialBalance">The initial balance of the account.</param>
    public CreateAccountCommand(string accountNumber, string accountHolder, string currency, decimal initialBalance)
        : this()
    {
        AccountNumber = accountNumber;
        AccountHolder = accountHolder;
        Currency = currency;
        InitialBalance = initialBalance;
    }

    /// <summary>Returns a string representation of the command.</summary>
    /// <returns>A string describing the command.</returns>
    public override string ToString()
        => $"CreateAccountCommand {{ AccountNumber={AccountNumber}, Holder={AccountHolder}, Currency={Currency}, Balance={InitialBalance} }}";
}

/// <summary>
/// Command to deposit funds into an account.
/// </summary>
public class DepositCommand
{
    /// <summary>Gets or sets the account identifier.</summary>
    public string AccountId { get; set; }

    /// <summary>Gets or sets the amount to deposit.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the reference for the deposit transaction.</summary>
    public string Reference { get; set; }

    /// <summary>Gets or sets the correlation id used to trace the command.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the command was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DepositCommand"/> class
    /// with default values and a fresh correlation id.
    /// </summary>
    public DepositCommand()
    {
        AccountId = string.Empty;
        Reference = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DepositCommand"/> class
    /// with the specified deposit details.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="reference">The reference for the deposit transaction.</param>
    public DepositCommand(string accountId, decimal amount, string reference)
        : this()
    {
        AccountId = accountId;
        Amount = amount;
        Reference = reference;
    }

    /// <summary>Returns a string representation of the command.</summary>
    /// <returns>A string describing the command.</returns>
    public override string ToString()
        => $"DepositCommand {{ AccountId={AccountId}, Amount={Amount}, Reference={Reference} }}";
}

/// <summary>
/// Command to withdraw funds from an account.
/// </summary>
public class WithdrawCommand
{
    /// <summary>Gets or sets the account identifier.</summary>
    public string AccountId { get; set; }

    /// <summary>Gets or sets the amount to withdraw.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the reference for the withdrawal transaction.</summary>
    public string Reference { get; set; }

    /// <summary>Gets or sets the correlation id used to trace the command.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the command was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WithdrawCommand"/> class
    /// with default values and a fresh correlation id.
    /// </summary>
    public WithdrawCommand()
    {
        AccountId = string.Empty;
        Reference = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WithdrawCommand"/> class
    /// with the specified withdrawal details.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="reference">The reference for the withdrawal transaction.</param>
    public WithdrawCommand(string accountId, decimal amount, string reference)
        : this()
    {
        AccountId = accountId;
        Amount = amount;
        Reference = reference;
    }

    /// <summary>Returns a string representation of the command.</summary>
    /// <returns>A string describing the command.</returns>
    public override string ToString()
        => $"WithdrawCommand {{ AccountId={AccountId}, Amount={Amount}, Reference={Reference} }}";
}

/// <summary>
/// Command to close an account.
/// </summary>
public class CloseAccountCommand
{
    /// <summary>Gets or sets the account identifier.</summary>
    public string AccountId { get; set; }

    /// <summary>Gets or sets the reason for closing the account.</summary>
    public string Reason { get; set; }

    /// <summary>Gets or sets the correlation id used to trace the command.</summary>
    public string CorrelationId { get; set; }

    /// <summary>Gets or sets the timestamp when the command was issued.</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloseAccountCommand"/> class
    /// with default values and a fresh correlation id.
    /// </summary>
    public CloseAccountCommand()
    {
        AccountId = string.Empty;
        Reason = string.Empty;
        CorrelationId = Guid.NewGuid().ToString();
        IssuedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CloseAccountCommand"/> class
    /// with the specified account closure details.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="reason">The reason for closing the account.</param>
    public CloseAccountCommand(string accountId, string reason)
        : this()
    {
        AccountId = accountId;
        Reason = reason;
    }

    /// <summary>Returns a string representation of the command.</summary>
    /// <returns>A string describing the command.</returns>
    public override string ToString()
        => $"CloseAccountCommand {{ AccountId={AccountId}, Reason={Reason} }}";
}
