// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotNetCqrsEventSourcing.Application.Commands;
using DotNetCqrsEventSourcing.Application.Queries;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// HTTP API controller for Account aggregate operations.
/// Separates command (write) and query (read) endpoints according to CQRS pattern.
/// Each endpoint is designed to be idempotent and handle domain errors gracefully.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountsController : BaseApiController
{
    private readonly IAccountService _accountService;
    private readonly IEventStore _eventStore;
    private readonly IProjectionService _projectionService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        IAccountService accountService,
        IEventStore eventStore,
        IProjectionService projectionService,
        ILogger<AccountsController> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// POST /accounts - Create a new bank account with initial balance.
    /// Returns 201 Created with the new account ID and event ID that created it.
    /// Validates owner name, initial balance, and currency before persisting the event.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateAccount request for owner: {Owner}", request.OwnerName);

        var command = new CreateAccountCommand(request.OwnerName, request.InitialBalance, request.Currency);
        var result = await _accountService.CreateAccountAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, errors = result.Errors });
        }

        return CreatedAtAction(nameof(GetAccountById), new { id = result.Value }, new
        {
            success = true,
            accountId = result.Value,
            message = "Account created successfully"
        });
    }

    /// <summary>
    /// GET /accounts/{id} - Retrieve account state from projection (read model).
    /// Returns 200 OK with current balance, owner, and transaction history.
    /// Uses event store replay if projection is missing, ensuring eventual consistency.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountById(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAccountById request for accountId: {AccountId}", id);

        try
        {
            var query = new GetAccountQuery(id);
            var account = await _accountService.GetAccountAsync(query, cancellationToken);

            return account is not null
                ? Ok(new { success = true, data = account })
                : NotFound(new { success = false, message = $"Account {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {AccountId}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// POST /accounts/{id}/deposit - Apply a deposit event to the account.
    /// Idempotent: applying the same deposit twice with same idempotency key succeeds once.
    /// Returns 200 OK with new balance and the event ID persisted.
    /// </summary>
    [HttpPost("{id}/deposit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deposit(
        [FromRoute] string id,
        [FromBody] TransactionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deposit request for accountId: {AccountId}, amount: {Amount}", id, request.Amount);

        var result = await _accountService.DepositAsync(id, request.Amount, idempotencyKey, cancellationToken);
        return Response(result);
    }

    /// <summary>
    /// POST /accounts/{id}/withdraw - Apply a withdrawal event to the account.
    /// Validates sufficient funds before emitting the event.
    /// Prevents negative balances through domain invariant checks in the aggregate.
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Withdraw(
        [FromRoute] string id,
        [FromBody] TransactionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Withdraw request for accountId: {AccountId}, amount: {Amount}", id, request.Amount);

        var result = await _accountService.WithdrawAsync(id, request.Amount, idempotencyKey, cancellationToken);
        return Response(result);
    }

    /// <summary>
    /// GET /accounts/{id}/events - Return all events for an account (complete audit trail).
    /// Useful for debugging, auditing, and verifying event sourcing integrity.
    /// Events are returned in causality order (oldest first).
    /// </summary>
    [HttpGet("{id}/events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccountEvents(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var events = await _eventStore.GetEventsAsync(id, cancellationToken);
            return Ok(new { success = true, eventCount = events.Count, events = events });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for account {AccountId}", id);
            return StatusCode(500, new { success = false, message = "Error retrieving events" });
        }
    }

    /// <summary>
    /// GET /accounts/{id}/replay - Trigger manual replay of events for this account.
    /// Rebuilds the projection from the event store, useful after projection bugs or data corruption.
    /// This is an admin operation that should be protected in production environments.
    /// </summary>
    [HttpPost("{id}/replay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplayAccountEvents(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Manual replay requested for accountId: {AccountId}", id);

        var events = await _eventStore.GetEventsAsync(id, cancellationToken);
        await _projectionService.RebuildProjectionAsync(id, events, cancellationToken);

        return Ok(new { success = true, message = $"Replayed {events.Count} events for account {id}" });
    }
}

public record CreateAccountRequest(string OwnerName, decimal InitialBalance, string Currency);
public record TransactionRequest(decimal Amount);
