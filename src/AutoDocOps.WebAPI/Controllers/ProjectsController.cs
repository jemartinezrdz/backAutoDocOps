using AutoDocOps.Application.Projects.Commands.CreateProject;
using AutoDocOps.Application.Projects.Queries.GetProjects;
using AutoDocOps.Application.Projects.Queries.GetProject;
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
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IMediator mediator, ILogger<ProjectsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get projects for an organization with pagination
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of projects</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetProjectsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetProjectsResponse>> GetProjects(
        [FromQuery, Required] Guid organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            // Validate user has access to this organization
            var userOrganizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(userOrganizationId) || 
                !Guid.TryParse(userOrganizationId, out var userOrgId) ||
                userOrgId != organizationId)
            {
                return Forbid("Access denied to this organization");
            }

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

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid page size",
                    Detail = "Page size must be between 1 and 100",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var query = new GetProjectsQuery(organizationId, page, pageSize);
            // Preservar contexto ASP.NET para HttpContext/User (ver docs Microsoft CA2007)
            #pragma warning disable CA2007
            var result = await _mediator.Send(query);
            #pragma warning restore CA2007

            _logger.RetrievedProjects(result.Projects.Count(), organizationId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.ErrorRetrievingProjects(organizationId, ex);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving projects",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get a specific project by ID
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <returns>Project details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProjectDto>> GetProject(Guid id)
    {
        try
        {
            _logger.RetrievingProject(id);
            
            var query = new GetProjectQuery(id);
            // Preservar contexto ASP.NET para HttpContext/User (ver docs Microsoft CA2007)
            #pragma warning disable CA2007
            var result = await _mediator.Send(query);
            #pragma warning restore CA2007
            
            return Ok(result.Project);
        }
        catch (ArgumentException ex)
        {
            _logger.ProjectNotFound(id, ex);
            
            return NotFound(new ProblemDetails
            {
                Title = "Project not found",
                Detail = $"Project with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.ErrorRetrievingProject(id, ex);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving the project",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    /// <param name="request">Project creation request</param>
    /// <returns>Created project</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateProjectResponse>> CreateProject([FromBody] CreateProjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var command = new CreateProjectCommand(
                request.Name,
                request.Description,
                request.RepositoryUrl,
                request.Branch ?? "main",
                request.OrganizationId,
                request.CreatedBy
            );

            // Preservar contexto ASP.NET para HttpContext/User (ver docs Microsoft CA2007)
            #pragma warning disable CA2007
            var result = await _mediator.Send(command);
            #pragma warning restore CA2007

            _logger.CreatedProject(result.Id, result.OrganizationId);

            return CreatedAtAction(
                nameof(GetProject),
                new { id = result.Id },
                result);
        }
        catch (ArgumentException ex)
        {
            _logger.InvalidProjectCreationRequest(ex);
            
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.ErrorCreatingProject(ex);
            
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while creating the project",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}

/// <summary>
/// Request model for creating a project
/// </summary>
public record CreateProjectRequest(
    [Required] string Name,
    string? Description,
    [Required] string RepositoryUrl,
    string? Branch,
    [Required] Guid OrganizationId,
    [Required] Guid CreatedBy
);

