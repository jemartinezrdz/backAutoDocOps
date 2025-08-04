using AutoDocOps.Application.Passports.Commands.GeneratePassport;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AutoDocOps.WebAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
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
        try
        {
            var command = new GeneratePassportCommand(
                request.ProjectId,
                request.Version ?? "1.0.0",
                request.Format ?? "markdown",
                request.GeneratedBy
            );

            var result = await _mediator.Send(command);

            _logger.LogInformation("Started documentation generation for project {ProjectId}, passport {PassportId}", 
                request.ProjectId, result.Id);

            // Return 202 Accepted since this is an async operation
            Response.Headers.Add("Location", $"/api/v1/passports/{result.Id}");
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
            // TODO: Implement GetLatestPassportByProjectIdQuery
            _logger.LogInformation("Retrieving generation status for project {ProjectId}", projectId);
            
            // Placeholder implementation
            var status = new GenerationStatusResponse(
                projectId,
                Guid.NewGuid(),
                "Generating",
                0,
                "Analyzing project structure...",
                DateTime.UtcNow,
                null,
                null
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
            // TODO: Implement CancelGenerationCommand
            _logger.LogInformation("Cancelling documentation generation for project {ProjectId}", projectId);
            
            return Ok(new { Message = "Generation cancelled successfully" });
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

