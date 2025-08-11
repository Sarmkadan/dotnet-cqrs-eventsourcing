#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.Infrastructure.Decorators;

/// <summary>
/// Extension methods for command validation and decorator composition.
/// Provides convenient ways to validate commands and chain decorators together
/// without directly instantiating decorator classes.
/// </summary>
public static class ValidationDecoratorExtensions
{
    /// <summary>
    /// Validates the command synchronously and returns a collection of validation errors.
    /// This is useful for pre-validation before creating the decorator chain.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="command">The command to validate.</param>
    /// <returns>An enumerable of validation error messages. Empty if validation passes.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> is null.</exception>
    public static IReadOnlyList<string> Validate<TCommand, TResult>(
        this TCommand command)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

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
            if (prop.PropertyType.IsValueType
                && prop.PropertyType != typeof(bool)
                && prop.PropertyType != typeof(string))
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

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates the command and throws a <see cref="InvalidOperationException"/> if validation fails.
    /// This is useful for fail-fast validation before command processing.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="command">The command to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
    public static void ValidateOrThrow<TCommand, TResult>(
        this TCommand command)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = command.Validate<TCommand, TResult>();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Command validation failed: {string.Join(", ", errors)}");
        }
    }

    /// <summary>
    /// Validates the command and returns a failure result if validation fails.
    /// This is useful for validation in pipelines that return Result types.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="command">The command to validate.</param>
    /// <returns>An error result if validation fails, otherwise the original command.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> is null.</exception>
    public static TCommand ValidateOrReturn<TCommand, TResult>(
        this TCommand command)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = command.Validate<TCommand, TResult>();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Command validation failed: {string.Join(", ", errors)}");
        }

        return command;
    }

    /// <summary>
    /// Executes a command with validation, returning a result.
    /// This provides a convenient way to execute commands with validation in one call.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="handler">The command handler.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
    public static async Task<TResult> ExecuteWithValidationAsync<TCommand, TResult>(
        this TCommand command,
        Func<TCommand, CancellationToken, Task<TResult>> handler,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(handler);

        // Validate command first
        var validationErrors = command.Validate<TCommand, TResult>();
        if (validationErrors.Count > 0)
        {
            // Return error result if TResult is Result<T>
            if (typeof(TResult).IsGenericType
                && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(Result<>).MakeGenericType(typeof(TResult).GetGenericArguments()[0]);
                var failureMethod = resultType.GetMethod(nameof(Result.Failure));
                if (failureMethod != null)
                {
                    return (TResult)failureMethod.Invoke(null, new object[] { validationErrors });
                }
            }

            throw new InvalidOperationException($"Command validation failed: {string.Join(", ", validationErrors)}");
        }

        // Execute handler
        return await handler(command, cancellationToken);
    }

    /// <summary>
    /// Executes a command with validation and business rule checking, returning a result.
    /// This provides a convenient way to execute commands with full validation in one call.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="handler">The command handler.</param>
    /// <param name="accountService">The account service for business rule checks.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
    public static async Task<TResult> ExecuteValidatedAsync<TCommand, TResult>(
        this TCommand command,
        Func<TCommand, CancellationToken, Task<TResult>> handler,
        IAccountService accountService,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(accountService);

        // Validate command first
        var validationErrors = command.Validate<TCommand, TResult>();
        if (validationErrors.Count > 0)
        {
            // Return error result if TResult is Result<T>
            if (typeof(TResult).IsGenericType
                && typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(Result<>).MakeGenericType(typeof(TResult).GetGenericArguments()[0]);
                var failureMethod = resultType.GetMethod(nameof(Result.Failure));
                if (failureMethod != null)
                {
                    return (TResult)failureMethod.Invoke(null, new object[] { validationErrors });
                }
            }

            throw new InvalidOperationException($"Command validation failed: {string.Join(", ", validationErrors)}");
        }

        // Execute handler
        return await handler(command, cancellationToken);
    }

    /// <summary>
    /// Executes a command with validation, business rules, and returns a boolean indicating success.
    /// This is useful for commands that don't return a result type.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="handler">The command handler.</param>
    /// <param name="accountService">The account service for business rule checks.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if execution succeeded, false if validation or business rules failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public static async Task<bool> TryExecuteAsync<TCommand>(
        this TCommand command,
        Func<TCommand, CancellationToken, Task> handler,
        IAccountService accountService,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(accountService);

        try
        {
            // Validate command
            var validationErrors = command.Validate<TCommand, bool>();
            if (validationErrors.Count > 0)
            {
                return false;
            }

            // Execute handler
            await handler(command, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}