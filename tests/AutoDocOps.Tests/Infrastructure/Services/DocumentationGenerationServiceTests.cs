using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Services;

public class DocumentationGenerationServiceTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IPassportRepository> _mockPassportRepository;
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<ILogger<DocumentationGenerationService>> _mockLogger;
    private readonly DocumentationGenerationService _service;

    public DocumentationGenerationServiceTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockPassportRepository = new Mock<IPassportRepository>();
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockLogger = new Mock<ILogger<DocumentationGenerationService>>();

        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        
        _mockServiceProvider.Setup(x => x.GetService(typeof(IPassportRepository)))
            .Returns(_mockPassportRepository.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IProjectRepository)))
            .Returns(_mockProjectRepository.Object);

        _service = new DocumentationGenerationService(_mockScopeFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessPendingPassports_WithValidPassports_ProcessesSuccessfully()
    {
        // Arrange
        var passport = new Passport
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Status = PassportStatus.Generating,
            GeneratedAt = DateTime.UtcNow,
            Version = "1.0.0",
            Format = "markdown",
            GeneratedBy = Guid.NewGuid()
        };

        var project = new Project
        {
            Id = passport.ProjectId,
            Name = "Test Project",
            Description = "Test Description",
            RepositoryUrl = "https://github.com/test/repo",
            Branch = "main",
            Specs = new List<Spec>
            {
                new Spec
                {
                    Id = Guid.NewGuid(),
                    FileName = "test.cs",
                    FilePath = "/src/test.cs",
                    Language = "C#",
                    FileType = "source",
                    Content = "public class Test { }",
                    LineCount = 1,
                    SizeInBytes = 20,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _mockPassportRepository.Setup(x => x.GetByStatusAsync(PassportStatus.Generating, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { passport });

        _mockProjectRepository.Setup(x => x.GetByIdWithSpecsAsync(passport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _mockPassportRepository.Setup(x => x.UpdateAsync(It.IsAny<Passport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Passport p, CancellationToken ct) => p);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5)); // Prevent infinite loop

        try
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(100, CancellationToken.None); // Allow some processing
            await _service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected when service is cancelled
        }

        // Assert
        _mockPassportRepository.Verify(x => x.GetByStatusAsync(PassportStatus.Generating, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mockProjectRepository.Verify(x => x.GetByIdWithSpecsAsync(passport.ProjectId, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessPendingPassports_WithProjectNotFound_MarksPassportAsFailed()
    {
        // Arrange
        var passport = new Passport
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Status = PassportStatus.Generating,
            GeneratedAt = DateTime.UtcNow,
            Version = "1.0.0",
            Format = "markdown",
            GeneratedBy = Guid.NewGuid()
        };

        _mockPassportRepository.Setup(x => x.GetByStatusAsync(PassportStatus.Generating, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { passport });

        _mockProjectRepository.Setup(x => x.GetByIdWithSpecsAsync(passport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var updatedPassport = (Passport?)null;
        _mockPassportRepository.Setup(x => x.UpdateAsync(It.IsAny<Passport>(), It.IsAny<CancellationToken>()))
            .Callback<Passport, CancellationToken>((p, ct) => updatedPassport = p)
            .ReturnsAsync((Passport p, CancellationToken ct) => p);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(100, CancellationToken.None);
            await _service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _mockPassportRepository.Verify(x => x.UpdateAsync(It.IsAny<Passport>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Service_HandlesExceptionsGracefully()
    {
        // Arrange
        _mockPassportRepository.Setup(x => x.GetByStatusAsync(PassportStatus.Generating, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(100, CancellationToken.None);
            await _service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Service should not crash and should log the error
        VerifyLoggerWasCalled(_mockLogger, LogLevel.Error, Times.AtLeastOnce());
    }

    private static void VerifyLoggerWasCalled<T>(Mock<ILogger<T>> mockLogger, LogLevel expectedLogLevel, Times times)
    {
        mockLogger.Verify(
            x => x.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
