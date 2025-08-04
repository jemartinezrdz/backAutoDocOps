using AutoDocOps.Application.Passports.Commands.GeneratePassport;
using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using Moq;
using Xunit;

namespace AutoDocOps.Tests.Passports.Commands;

public class GeneratePassportHandlerTests
{
    private readonly Mock<IPassportRepository> _mockPassportRepository;
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly GeneratePassportHandler _handler;

    public GeneratePassportHandlerTests()
    {
        _mockPassportRepository = new Mock<IPassportRepository>();
        _mockProjectRepository = new Mock<IProjectRepository>();
        _handler = new GeneratePassportHandler(_mockPassportRepository.Object, _mockProjectRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesPassportWithGeneratingStatus()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var generatedBy = Guid.NewGuid();
        var project = new Project { Id = projectId, Name = "Test Project" };
        
        _mockProjectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var createdPassport = new Passport
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Version = "1.0.0",
            Format = "markdown",
            Status = PassportStatus.Generating,
            GeneratedBy = generatedBy,
            GeneratedAt = DateTime.UtcNow
        };

        _mockPassportRepository.Setup(x => x.CreateAsync(It.IsAny<Passport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPassport);

        var command = new GeneratePassportCommand(projectId, "1.0.0", "markdown", generatedBy);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.ProjectId);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("markdown", result.Format);
        Assert.Equal(PassportStatus.Generating, result.Status);
        Assert.Equal(generatedBy, result.GeneratedBy);

        _mockProjectRepository.Verify(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
        _mockPassportRepository.Verify(x => x.CreateAsync(It.IsAny<Passport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ThrowsArgumentException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var generatedBy = Guid.NewGuid();
        
        _mockProjectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new GeneratePassportCommand(projectId, "1.0.0", "markdown", generatedBy);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _handler.Handle(command, CancellationToken.None));
        
        Assert.Contains($"Project with ID {projectId} not found", exception.Message);
    }

    [Theory]
    [InlineData("", "markdown")]
    [InlineData("1.0.0", "")]
    [InlineData(null, "markdown")]
    [InlineData("1.0.0", null)]
    public async Task Handle_InvalidInput_HandlesGracefully(string? version, string? format)
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var generatedBy = Guid.NewGuid();
        var project = new Project { Id = projectId, Name = "Test Project" };
        
        _mockProjectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var createdPassport = new Passport
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Version = version ?? "1.0.0",
            Format = format ?? "markdown",
            Status = PassportStatus.Generating,
            GeneratedBy = generatedBy,
            GeneratedAt = DateTime.UtcNow
        };

        _mockPassportRepository.Setup(x => x.CreateAsync(It.IsAny<Passport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPassport);

        var command = new GeneratePassportCommand(projectId, version ?? "1.0.0", format ?? "markdown", generatedBy);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PassportStatus.Generating, result.Status);
    }
}
