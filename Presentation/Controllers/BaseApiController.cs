// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using Microsoft.AspNetCore.Mvc;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// Base controller class providing standardized response handling for all API endpoints.
/// Ensures consistent HTTP status codes, error formatting, and result serialization across the API.
/// Implements Result pattern to avoid null reference exceptions and provide explicit success/failure states.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Converts a generic Result{T} to an appropriate IActionResult with proper HTTP status codes.
    /// Success: 200 OK with data; Failure: 400 BadRequest with error messages.
    /// This pattern enforces that all endpoints use the Result monad for predictable error handling.
    /// </summary>
    protected IActionResult Response<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new ApiResponse<T>
            {
                Success = true,
                Data = result.Value,
                Message = "Operation completed successfully"
            });
        }

        return BadRequest(new ApiResponse<T>
        {
            Success = false,
            Errors = result.Errors.ToList(),
            Message = "Operation failed. See errors for details."
        });
    }

    /// <summary>
    /// Converts a non-generic Result to an appropriate IActionResult.
    /// This is used for commands that don't return domain data (e.g., Delete, Update operations).
    /// </summary>
    protected IActionResult Response(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true, message = "Operation completed successfully" });
        }

        return BadRequest(new { success = false, errors = result.Errors.ToList() });
    }

    /// <summary>
    /// Helper to return 201 Created with location header.
    /// Used by POST endpoints that create new resources - follows REST conventions.
    /// </summary>
    protected IActionResult Created<T>(string resourceId, Result<T> result)
    {
        if (!result.IsSuccess)
        {
            return BadRequest(new { success = false, errors = result.Errors });
        }

        return CreatedAtAction(nameof(Created), new { id = resourceId }, new ApiResponse<T>
        {
            Success = true,
            Data = result.Value
        });
    }

    /// <summary>
    /// Returns 204 No Content for successful operations that don't return data.
    /// </summary>
    protected IActionResult NoContent(Result result)
    {
        return result.IsSuccess ? StatusCode((int)HttpStatusCode.NoContent) : BadRequest(result.Errors);
    }
}

public record ApiResponse<T>(bool Success = false, T? Data = default, string? Message = null, List<string>? Errors = null);
