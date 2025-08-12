using AutoDocOps.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Services;

/// <summary>
/// Security-focused tests for BillingService to ensure no NotImplementedException in production
/// </summary>
public class BillingServiceSecurityTests
{
    private readonly Mock<ILogger<BillingService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly BillingService _billingService;
    private readonly Mock<Stripe.IStripeClient> _mockStripeClient;

    public BillingServiceSecurityTests()
    {
        _mockLogger = new Mock<ILogger<BillingService>>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockStripeClient = new Mock<Stripe.IStripeClient>();
        
        // Mock required Stripe configuration
        _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns("sk_test_fake_key_for_testing");
        
    // Pass mocked StripeClient required by BillingService constructor
    _billingService = new BillingService(_mockLogger.Object, _mockConfiguration.Object, _mockStripeClient.Object);
    }

    [Fact]
    public async Task CancelSubscriptionAsyncDoesNotThrowNotImplementedException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        // Act & Assert - Should not throw NotImplementedException
    var exception = await Record.ExceptionAsync(() => _billingService.CancelSubscriptionAsync(organizationId)).ConfigureAwait(true);
        Assert.Null(exception);
    }

    [Fact]
    public async Task CancelSubscriptionAsyncWithNoSubscriptionReturnsFalseSafely()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        // Act
    var result = await _billingService.CancelSubscriptionAsync(organizationId).ConfigureAwait(true);

        // Assert
        Assert.False(result);
    }

    // Removed legacy warning assertion test: implementation now logs specific placeholder events via source-generated log methods.

    [Fact]
    public async Task CreateCheckoutSessionAsyncDoesNotThrowNotImplementedException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var planId = "starter";
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act & Assert - Should throw StripeException due to test key, but not NotImplementedException
        var exception = await Record.ExceptionAsync(() => 
            _billingService.CreateCheckoutSessionAsync(organizationId, planId, successUrl, cancelUrl)).ConfigureAwait(true);
        
        // Should get Stripe API error, not NotImplementedException
        Assert.NotNull(exception);
        Assert.IsNotType<NotImplementedException>(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid_plan")]
    [InlineData(null)]
    public async Task CreateCheckoutSessionAsyncWithInvalidPlanHandlesGracefully(string? invalidPlan)
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act & Assert - Should handle invalid plans gracefully
        var exception = await Record.ExceptionAsync(() => 
            _billingService.CreateCheckoutSessionAsync(organizationId, invalidPlan!, successUrl, cancelUrl)).ConfigureAwait(true);
        
        // Should get ArgumentException for unknown plan, not NotImplementedException
        if (exception != null)
        {
            Assert.IsNotType<NotImplementedException>(exception);
        }
    }

    [Fact]
    public async Task CancelSubscriptionAsyncWithEmptyGuidHandlesGracefully()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act & Assert - Should not throw
    var exception = await Record.ExceptionAsync(() => _billingService.CancelSubscriptionAsync(emptyGuid)).ConfigureAwait(true);
        Assert.Null(exception);
    }

    [Fact]
    public void BillingServiceConstructorRequiresValidStripeKey()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<BillingService>>();
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["Stripe:SecretKey"]).Returns((string?)null);

        // Act & Assert - Should throw InvalidOperationException for missing config
    var mockStripe = new Mock<Stripe.IStripeClient>();
    var exception = Record.Exception(() => new BillingService(mockLogger.Object, mockConfig.Object, mockStripe.Object));
        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("Stripe SecretKey", exception.Message);
    }

    [Theory]
    [InlineData("starter")]
    [InlineData("growth")]
    public async Task CreateCheckoutSessionAsyncWithValidPlansDoesNotThrowNotImplementedException(string planId)
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act & Assert - Should get Stripe API error, not NotImplementedException
        var exception = await Record.ExceptionAsync(() => 
            _billingService.CreateCheckoutSessionAsync(organizationId, planId, successUrl, cancelUrl)).ConfigureAwait(true);
        
        Assert.NotNull(exception);
        Assert.IsNotType<NotImplementedException>(exception);
    }
}
