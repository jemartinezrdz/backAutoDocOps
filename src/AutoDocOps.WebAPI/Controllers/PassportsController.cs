using AutoDocOps.Application.Passports.Queries.GetPassport;
using AutoDocOps.Application.Passports.Queries.GetPassportsByProject;
using AutoDocOps.Application.Passports.Commands.DeletePassport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AutoDocOps.WebAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "DeveloperOrAdmin")]
public class PassportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PassportsController> _logger;

    public PassportsController(IMediator mediator, ILogger<PassportsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific passport by ID
    /// </summary>
    /// <param name="id">Passport ID</param>
    /// <returns>Passport details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PassportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PassportDto>> GetPassport(Guid id)
    {
        try
        {
            _logger.RetrievingPassport(id);
            
            var query = new GetPassportQuery(id);
            // Preservar contexto ASP.NET para HttpContext/User (ver docs Microsoft CA2007)
            #pragma warning disable CA2007
            var result = await _mediator.Send(query);
            #pragma warning restore CA2007
            
            var passportDto = new PassportDto(
                result.Id,
                result.ProjectId,
                result.Version,
                result.Format,
                result.Status.ToString(),
                result.GeneratedAt,
                result.CompletedAt,
                result.GeneratedBy,
                result.SizeInBytes,
                result.ErrorMessage
            );
            
            return Ok(passportDto);
        }
        catch (ArgumentException ex)
        {
            _logger.PassportNotFound(id, ex);
            
            return NotFound(new ProblemDetails
            {
                Title = "Passport not found",
                Detail = $"Passport with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.ErrorRetrievingPassport(id, ex);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving the passport",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get passports for a specific project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 50)</param>
    /// <returns>Paginated list of passports</returns>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(PassportListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PassportListResponse>> GetPassportsByProject(
        Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            // Validate parameters
            if (page < 1)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid page number",
                    Detail = "Page number must be greater than 0",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (pageSize < 1 || pageSize > 50)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid page size",
                    Detail = "Page size must be between 1 and 50",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.RetrievingPassportsForProject(projectId);
            
            var query = new GetPassportsByProjectQuery(projectId, page, pageSize);
            // Preservar contexto ASP.NET para HttpContext/User (ver docs Microsoft CA2007)
            #pragma warning disable CA2007
            var result = await _mediator.Send(query);
            #pragma warning restore CA2007
            
            var passportDtos = result.Passports.Select(p => new PassportDto(
                p.Id,
                p.ProjectId,
                p.Version,
                p.Format,
                p.Status.ToString(),
                p.GeneratedAt,
                p.CompletedAt,
                p.GeneratedBy,
                p.SizeInBytes,
                p.ErrorMessage
            ));

            var response = new PassportListResponse(
                passportDtos,
                result.TotalCount,
                page,
                pageSize
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.ErrorRetrievingPassportsForProject(projectId, ex);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving passports",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Delete a passport
    /// </summary>
    /// <param name="id">Passport ID</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeletePassport(Guid id)
    {
        try
        {
            _logger.DeletingPassport(id);
            
            var command = new DeletePassportCommand(id);
            // Preservar contexto ASP.NET para HttpContext/User (ver docs Microsoft CA2007)
            #pragma warning disable CA2007
            var result = await _mediator.Send(command);
            #pragma warning restore CA2007
            
            if (!result.Success)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Passport not found",
                    Detail = result.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.ErrorDeletingPassport(id, ex);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while deleting the passport",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Download passport content
    /// </summary>
    /// <param name="id">Passport ID</param>
    /// <param name="format">Download format (original, pdf, html)</param>
    /// <returns>Passport content file</returns>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DownloadPassport(Guid id, [FromQuery] string format = "original")
    {
        try
        {
            _logger.DownloadingPassport(id, format);
            
            var query = new GetPassportQuery(id);
            // Preservar contexto ASP.NET para HttpContext/User (ver docs Microsoft CA2007)
            #pragma warning disable CA2007
            var result = await _mediator.Send(query);
            #pragma warning restore CA2007
            
            var fileName = $"passport-{result.Version}.{result.Format}";
                        var contentType = result.Format.ToLowerInvariant() switch
            {
                "pdf" => "application/pdf",
                "html" => "text/html",
                "markdown" => "text/markdown",
                _ => "text/plain"
            };

            var content = System.Text.Encoding.UTF8.GetBytes(result.DocumentationContent);
            
            return File(content, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.PassportNotFound(id, ex);
            
            return NotFound(new ProblemDetails
            {
                Title = "Passport not found",
                Detail = $"Passport with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.ErrorDownloadingPassport(id, ex);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while downloading the passport",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}

/// <summary>
/// Passport data transfer object
/// </summary>
public record PassportDto(
    Guid Id,
    Guid ProjectId,
    string Version,
    string Format,
    string Status,
    DateTime GeneratedAt,
    DateTime? CompletedAt,
    Guid GeneratedBy,
    long SizeInBytes,
    string? ErrorMessage
);

/// <summary>
/// Response model for passport list
/// </summary>
public record PassportListResponse(
    IEnumerable<PassportDto> Passports,
    int TotalCount,
    int Page,
    int PageSize
);

