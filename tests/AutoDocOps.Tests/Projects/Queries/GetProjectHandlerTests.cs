using AutoDocOps.Application.Projects.Queries.GetProject;
using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using Moq;
using Xunit;

namespace AutoDocOps.Tests.Projects.Queries;

public class GetProjectHandlerTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly GetProjectHandler _handler;

    public GetProjectHandlerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockCacheService = new Mock<ICacheService>();
        _handler = new GetProjectHandler(_mockProjectRepository.Object, _mockCacheService.Object);
    }

    [Fact]
    public async Task Handle_ValidProjectId_ReturnsProjectResponse()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            Description = "Test Description",
            RepositoryUrl = "https://github.com/test/repo",
            Branch = "main",
            OrganizationId = organizationId,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _mockProjectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var query = new GetProjectQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Project);
        Assert.Equal(projectId, result.Project.Id);
        Assert.Equal("Test Project", result.Project.Name);
        Assert.Equal("Test Description", result.Project.Description);
        Assert.Equal("https://github.com/test/repo", result.Project.RepositoryUrl);
        Assert.Equal("main", result.Project.Branch);
        Assert.Equal(organizationId, result.Project.OrganizationId);
        Assert.Equal(createdBy, result.Project.CreatedBy);
        Assert.True(result.Project.IsActive);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ThrowsArgumentException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _mockProjectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var query = new GetProjectQuery(projectId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _handler.Handle(query, CancellationToken.None));
        
        Assert.Contains($"Project with ID {projectId} not found", exception.Message);
    }
}
