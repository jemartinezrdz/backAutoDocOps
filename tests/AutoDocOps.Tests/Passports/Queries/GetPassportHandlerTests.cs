using AutoDocOps.Application.Passports.Queries.GetPassport;
using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using Moq;
using Xunit;

namespace AutoDocOps.Tests.Passports.Queries;

public class GetPassportHandlerTests
{
    private readonly Mock<IPassportRepository> _mockPassportRepository;
    private readonly GetPassportHandler _handler;

    public GetPassportHandlerTests()
    {
        _mockPassportRepository = new Mock<IPassportRepository>();
        _handler = new GetPassportHandler(_mockPassportRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidPassportId_ReturnsPassportResponse()
    {
        // Arrange
        var passportId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var generatedBy = Guid.NewGuid();
        var passport = new Passport
        {
            Id = passportId,
            ProjectId = projectId,
            Version = "1.0.0",
            DocumentationContent = "Test documentation",
            Format = "markdown",
            Status = PassportStatus.Completed,
            GeneratedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            GeneratedBy = generatedBy,
            SizeInBytes = 1000
        };

        _mockPassportRepository.Setup(x => x.GetByIdAsync(passportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(passport);

        var query = new GetPassportQuery(passportId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(passportId, result.Id);
        Assert.Equal(projectId, result.ProjectId);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("Test documentation", result.DocumentationContent);
        Assert.Equal("markdown", result.Format);
        Assert.Equal(PassportStatus.Completed, result.Status);
        Assert.Equal(generatedBy, result.GeneratedBy);
        Assert.Equal(1000, result.SizeInBytes);
    }

    [Fact]
    public async Task Handle_PassportNotFound_ThrowsArgumentException()
    {
        // Arrange
        var passportId = Guid.NewGuid();
        _mockPassportRepository.Setup(x => x.GetByIdAsync(passportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Passport?)null);

        var query = new GetPassportQuery(passportId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _handler.Handle(query, CancellationToken.None));
        
        Assert.Contains($"Passport with ID {passportId} not found", exception.Message);
    }
}
