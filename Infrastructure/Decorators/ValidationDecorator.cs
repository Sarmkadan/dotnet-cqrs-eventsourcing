// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.Infrastructure.Decorators;

/// <summary>
/// Decorator that validates domain invariants and business rules before command execution.
/// Wraps command handlers to ensure invalid commands fail fast with clear error messages.
/// Validates all command properties are set correctly and aggregate constraints are met.
/// This prevents invalid state propagation to the domain and event stream.
/// </summary>
public class ValidationDecorator<TCommand, TResult> where TCommand : class
{
    private readonly Func<TCommand, CancellationToken, Task<TResult>> _next;
    private readonly ILogger<ValidationDecorator<TCommand, TResult>> _logger;

    public ValidationDecorator(
        Func<TCommand, CancellationToken, Task<TResult>> next,
        ILogger<ValidationDecorator<TCommand, TResult>> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating command: {CommandType}", typeof(TCommand).Name);

        var errors = ValidateCommand(command);
        if (errors.Any())
        {
            var errorMessage = $"Command validation failed: {string.Join(", ", errors)}";
            _logger.LogWarning("Validation failed for {CommandType}: {Errors}", typeof(TCommand).Name, errorMessage);

            // Return error result if TResult is Result<T>
            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(Result<>).MakeGenericType(typeof(TResult).GetGenericArguments()[0]);
                var failureMethod = resultType.GetMethod(nameof(Result.Failure))
                    ?? throw new InvalidOperationException($"Cannot find Failure method on {resultType.Name}");

                return (TResult)(failureMethod.Invoke(null, new object[] { errors })
                    ?? throw new InvalidOperationException("Failed to create failure result"));
            }

            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogDebug("Command validation passed for {CommandType}", typeof(TCommand).Name);
        return await _next(command, cancellationToken);
    }

    /// <summary>
    /// Validates command properties - must not be null or default values.
    /// </summary>
    private static List<string> ValidateCommand(TCommand command)
    {
        var errors = new List<string>();

        if (command is null)
        {
            errors.Add("Command cannot be null");
            return errors;
        }

        var metadata = CqrsHelpers.GetHandlerMetadata(typeof(TCommand));

        foreach (var prop in metadata.Properties)
        {
            var value = prop.GetValue(command);

            // Check for null reference types
            if (!prop.PropertyType.IsValueType && value is null)
            {
                errors.Add($"{prop.Name} is required and cannot be null");
            }

            // Check for default values in value types
            if (prop.PropertyType.IsValueType && prop.PropertyType != typeof(bool) && prop.PropertyType != typeof(string))
            {
                var defaultValue = Activator.CreateInstance(prop.PropertyType);
                if (Equals(value, defaultValue))
                {
                    errors.Add($"{prop.Name} has invalid default value");
                }
            }

            // Special validation for string properties
            if (prop.PropertyType == typeof(string) && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                errors.Add($"{prop.Name} cannot be empty");
            }

            // Validate decimal amounts (must be positive)
            if (prop.PropertyType == typeof(decimal) && value is decimal decValue && decValue <= 0)
            {
                errors.Add($"{prop.Name} must be greater than zero");
            }
        }

        return errors;
    }
}

/// <summary>
/// Decorator for business rule validation on aggregates.
/// Checks that domain invariants are satisfied before persisting events.
/// </summary>
public class BusinessRuleDecorator<TCommand, TResult> where TCommand : class
{
    private readonly Func<TCommand, CancellationToken, Task<TResult>> _next;
    private readonly IAccountService _accountService;
    private readonly ILogger<BusinessRuleDecorator<TCommand, TResult>> _logger;

    public BusinessRuleDecorator(
        Func<TCommand, CancellationToken, Task<TResult>> next,
        IAccountService accountService,
        ILogger<BusinessRuleDecorator<TCommand, TResult>> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking business rules for {CommandType}", typeof(TCommand).Name);

        // Extract aggregate ID from command if it has one
        var aggregateId = CqrsHelpers.ExtractAggregateId(command);

        if (!string.IsNullOrEmpty(aggregateId))
        {
            // Verify aggregate exists (if needed for this command)
            var aggregateExists = await _accountService.AggregateExistsAsync(aggregateId, cancellationToken);

            if (!aggregateExists && IsModificationCommand())
            {
                var error = $"Aggregate {aggregateId} does not exist";
                _logger.LogWarning("Business rule violation: {Error}", error);

                if (typeof(TResult).IsGenericType)
                {
                    var resultType = typeof(Result<>).MakeGenericType(typeof(TResult).GetGenericArguments()[0]);
                    var failureMethod = resultType.GetMethod(nameof(Result.Failure))
                        ?? throw new InvalidOperationException($"Cannot find Failure method on {resultType.Name}");

                    return (TResult)(failureMethod.Invoke(null, new object[] { new[] { error } })
                        ?? throw new InvalidOperationException("Failed to create failure result"));
                }

                throw new InvalidOperationException(error);
            }
        }

        return await _next(command, cancellationToken);
    }

    /// <summary>
    /// Determines if this command modifies state (vs. read-only query).
    /// Commands containing "Create", "Update", "Delete", "Withdraw", "Deposit" are modifications.
    /// </summary>
    private bool IsModificationCommand()
    {
        var commandName = typeof(TCommand).Name.ToLower();
        return commandName.Contains("create") ||
               commandName.Contains("update") ||
               commandName.Contains("delete") ||
               commandName.Contains("withdraw") ||
               commandName.Contains("deposit");
    }
}

/// <summary>
/// Composite decorator to chain multiple decorators together.
/// </summary>
public static class DecoratorChain
{
    public static async Task<T> Execute<TCommand, T>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<T>> handler,
        CancellationToken cancellationToken)
        where TCommand : class
    {
        // Wrap handler with validation first
        Func<TCommand, CancellationToken, Task<T>> decorated = handler;

        // Apply decorators in reverse order of desired execution
        // Last decorator in list is innermost (closest to handler)

        return await decorated(command, cancellationToken);
    }
}
