using AutoDocOps.Application.Passports.Commands.GeneratePassport;
using AutoDocOps.Application.Passports.Queries.GetGenerationStatus;
using AutoDocOps.Application.Passports.Commands.CancelGeneration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AutoDocOps.WebAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "DeveloperOrAdmin")]
public class GenerateController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GenerateController> _logger;

    public GenerateController(IMediator mediator, ILogger<GenerateController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generate documentation for a project
    /// </summary>
    /// <param name="request">Documentation generation request</param>
    /// <returns>Generation job details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GeneratePassportResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GeneratePassportResponse>> GenerateDocumentation([FromBody] GenerateDocumentationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
                        var command = new GeneratePassportCommand(
                request.ProjectId,
                request.Version ?? "1.0.0",
                request.Format ?? "markdown",
                request.GeneratedBy
            );

            var result = await _mediator.Send(command).ConfigureAwait(false);

            _logger.LogInformation("Started documentation generation for project {ProjectId}, passport {PassportId}", 
                request.ProjectId, result.Id);

            // Return 202 Accepted since this is an async operation
            Response.Headers.Append("Location", $"/api/v1/passports/{result.Id}");
            return Accepted(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid documentation generation request for project {ProjectId}", request.ProjectId);
            
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating documentation for project {ProjectId}", request.ProjectId);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while starting documentation generation",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get generation status and progress
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Generation status</returns>
    [HttpGet("status/{projectId:guid}")]
    [ProducesResponseType(typeof(GenerationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerationStatusResponse>> GetGenerationStatus(Guid projectId)
    {
        try
        {
            _logger.LogInformation("Retrieving generation status for project {ProjectId}", projectId);
            
            // Get the latest passport for the project to check status
            var passports = await _mediator.Send(new AutoDocOps.Application.Passports.Queries.GetPassportsByProject.GetPassportsByProjectQuery(projectId, 1, 1));
            
            if (!passports.Passports.Any())
            {
                return NotFound(new ProblemDetails
                {
                    Title = "No generation found",
                    Detail = $"No documentation generation found for project {projectId}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var latestPassport = passports.Passports.First();
            var query = new GetGenerationStatusQuery(latestPassport.Id);
            var result = await _mediator.Send(query);
            
            var status = new GenerationStatusResponse(
                projectId,
                result.PassportId,
                result.Status.ToString(),
                result.ProgressPercentage,
                result.CurrentStep ?? "Processing...",
                DateTime.UtcNow,
                result.EstimatedCompletion,
                result.ErrorMessage
            );

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving generation status for project {ProjectId}", projectId);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving generation status",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Cancel documentation generation
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Cancellation result</returns>
    [HttpDelete("cancel/{projectId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CancelGeneration(Guid projectId)
    {
        try
        {
            _logger.LogInformation("Cancelling documentation generation for project {ProjectId}", projectId);
            
            // Get the latest generating passport for the project
            var passports = await _mediator.Send(new AutoDocOps.Application.Passports.Queries.GetPassportsByProject.GetPassportsByProjectQuery(projectId, 1, 1));
            
            if (!passports.Passports.Any())
            {
                return NotFound(new ProblemDetails
                {
                    Title = "No generation found",
                    Detail = $"No active generation found for project {projectId}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var latestPassport = passports.Passports.First();
            
            // Get cancellation requester from authenticated user context
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var cancelledBy))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "Authenticated user ID not found or invalid.",
                    Status = StatusCodes.Status401Unauthorized
                });
            }
            
            var command = new CancelGenerationCommand(latestPassport.Id, cancelledBy);
            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Cannot cancel generation",
                    Detail = result.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            
            return Ok(new { Message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling generation for project {ProjectId}", projectId);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while cancelling generation",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}

/// <summary>
/// Request model for generating documentation
/// </summary>
public record GenerateDocumentationRequest(
    [Required] Guid ProjectId,
    string? Version,
    string? Format, // markdown, html, pdf
    [Required] Guid GeneratedBy
);

/// <summary>
/// Response model for generation status
/// </summary>
public record GenerationStatusResponse(
    Guid ProjectId,
    Guid PassportId,
    string Status, // Generating, Completed, Failed, Cancelled
    int ProgressPercentage,
    string CurrentStep,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage
);

