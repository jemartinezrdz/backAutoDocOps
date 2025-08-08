using AutoDocOps.Application.Projects.Commands.CreateProject;
using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using Moq;
using Xunit;

namespace AutoDocOps.Tests.Projects.Commands;

public class CreateProjectHandlerTests
{
    private const string TestProjectName = "Test Project";
    private const string TestProjectDescription = "Test Description";  
    private const string TestRepositoryUrl = "https://github.com/test/repo";
    private const string TestBranch = "main";
    
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly CreateProjectHandler _handler;

    public CreateProjectHandlerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _handler = new CreateProjectHandler(_mockProjectRepository.Object);
    }

    [Fact(Timeout = 2000)]
    public async Task Handle_ValidCommand_ReturnsCreateProjectResponse()
    {
        // Arrange
        var command = new CreateProjectCommand(
            TestProjectName,
            TestProjectDescription,
            TestRepositoryUrl,
            TestBranch,
            Guid.NewGuid(),
            Guid.NewGuid()
        );

        var expectedProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            RepositoryUrl = command.RepositoryUrl,
            Branch = command.Branch,
            OrganizationId = command.OrganizationId,
            CreatedBy = command.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _mockProjectRepository
            .Setup(x => x.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProject);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedProject.Id, result.Id);
        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.Description, result.Description);
        Assert.Equal(command.RepositoryUrl, result.RepositoryUrl);
        Assert.Equal(command.Branch, result.Branch);
        Assert.Equal(command.OrganizationId, result.OrganizationId);
        Assert.Equal(command.CreatedBy, result.CreatedBy);

        _mockProjectRepository.Verify(
            x => x.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(Timeout = 2000)]
    public async Task Handle_ValidCommand_CreatesProjectWithCorrectProperties()
    {
        // Arrange
        var command = new CreateProjectCommand(
            TestProjectName,
            TestProjectDescription,
            TestRepositoryUrl,
            TestBranch,
            Guid.NewGuid(),
            Guid.NewGuid()
        );

        Project? capturedProject = null;
        _mockProjectRepository
            .Setup(x => x.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((project, _) => capturedProject = project)
            .ReturnsAsync((Project project, CancellationToken _) => project);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedProject);
        Assert.NotEqual(Guid.Empty, capturedProject.Id);
        Assert.Equal(command.Name, capturedProject.Name);
        Assert.Equal(command.Description, capturedProject.Description);
        Assert.Equal(command.RepositoryUrl, capturedProject.RepositoryUrl);
        Assert.Equal(command.Branch, capturedProject.Branch);
        Assert.Equal(command.OrganizationId, capturedProject.OrganizationId);
        Assert.Equal(command.CreatedBy, capturedProject.CreatedBy);
        Assert.True(capturedProject.IsActive);
        Assert.True(capturedProject.CreatedAt <= DateTime.UtcNow);
        Assert.True(capturedProject.UpdatedAt <= DateTime.UtcNow);
    }
}

