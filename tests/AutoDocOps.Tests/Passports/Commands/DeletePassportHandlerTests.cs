using AutoDocOps.Application.Passports.Commands.DeletePassport;
using AutoDocOps.Domain.Interfaces;
using Moq;
using Xunit;

namespace AutoDocOps.Tests.Passports.Commands;

public class DeletePassportHandlerTests
{
    private readonly Mock<IPassportRepository> _mockPassportRepository;
    private readonly DeletePassportHandler _handler;

    public DeletePassportHandlerTests()
    {
        _mockPassportRepository = new Mock<IPassportRepository>();
        _handler = new DeletePassportHandler(_mockPassportRepository.Object);
    }

    [Fact(Timeout = 2000)]
    public async Task Handle_ExistingPassport_ReturnsSuccessResponse()
    {
        // Arrange
        var passportId = Guid.NewGuid();
        
        _mockPassportRepository.Setup(x => x.DeleteAsync(passportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DeletePassportCommand(passportId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Passport deleted successfully", result.Message);

        _mockPassportRepository.Verify(x => x.DeleteAsync(passportId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(Timeout = 2000)]
    public async Task Handle_NonExistingPassport_ReturnsFailureResponse()
    {
        // Arrange
        var passportId = Guid.NewGuid();
        
        _mockPassportRepository.Setup(x => x.DeleteAsync(passportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new DeletePassportCommand(passportId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Passport not found or could not be deleted", result.Message);

        _mockPassportRepository.Verify(x => x.DeleteAsync(passportId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(Timeout = 2000)]
    public async Task Handle_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var passportId = Guid.NewGuid();
        var expectedException = new InvalidOperationException("Database error");
        
        _mockPassportRepository.Setup(x => x.DeleteAsync(passportId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var command = new DeletePassportCommand(passportId);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _handler.Handle(command, CancellationToken.None));
        
        Assert.Same(expectedException, actualException);
    }
}
