using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using StripeSubscription = Stripe.Subscription;
using System.Security.Cryptography;
using System.Text;

namespace AutoDocOps.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly ILogger<BillingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _stripeApiKey;

    public BillingService(ILogger<BillingService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _stripeApiKey = _configuration["Stripe:SecretKey"] 
            ?? throw new InvalidOperationException("Stripe SecretKey is not configured");
    }

    /// <summary>
    /// Helper to hash/anonymize organization IDs for secure logging
    /// </summary>
    private static string AnonymizeOrganizationId(Guid organizationId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(organizationId.ToString()));
        // Use first 12 hex chars for better collision resistance while maintaining readability
        return Convert.ToHexString(bytes)[..12];
    }

    public async Task HandleAsync(Event stripeEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Stripe event: {EventType} with ID: {EventId}", 
                stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent, cancellationToken);
                    break;
                
                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceeded(stripeEvent, cancellationToken);
                    break;
                
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent, cancellationToken);
                    break;
                
                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe event: {EventType} with ID: {EventId}", 
                stripeEvent.Type, stripeEvent.Id);
            throw;
        }
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid organizationId, string planId, 
        string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = GetStripePriceId(planId),
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "organization_id", organizationId.ToString() },
                    { "plan_id", planId }
                }
            };

            var service = new SessionService(new StripeClient(_stripeApiKey));
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Created checkout session {SessionId} for anonymized org {AnonymizedOrgId}", 
                session.Id, AnonymizeOrganizationId(organizationId));
            
            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for anonymized org {AnonymizedOrgId}", AnonymizeOrganizationId(organizationId));
            throw;
        }
    }

    public async Task<bool> CancelSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Lookup subscription in database for the given organization
            // Placeholder: Assume we have a method GetSubscriptionIdForOrganization
            string? subscriptionId = await GetSubscriptionIdForOrganization(organizationId, cancellationToken);
            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogWarning("No subscription found for anonymized org {AnonymizedOrgId}", AnonymizeOrganizationId(organizationId));
                return false;
            }

            var subscriptionService = new SubscriptionService(new StripeClient(_stripeApiKey));
            var canceledSubscription = await subscriptionService.CancelAsync(subscriptionId, null, cancellationToken: cancellationToken);

            // TODO: Update subscription status in database
            await UpdateSubscriptionStatus(organizationId, canceledSubscription.Id, "canceled", cancellationToken);

            _logger.LogInformation("Canceled subscription {SubscriptionId} for anonymized org {AnonymizedOrgId}", canceledSubscription.Id, AnonymizeOrganizationId(organizationId));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription for anonymized org {AnonymizedOrgId}", AnonymizeOrganizationId(organizationId));
            return false;
        }
    }

    // Placeholder for database lookup - safe fallback for production
    private async Task<string?> GetSubscriptionIdForOrganization(Guid organizationId, CancellationToken cancellationToken)
    {
        // TODO (issue #123): Replace with actual repository query.
        _logger.LogWarning(
            "GetSubscriptionIdForOrganization called for anonymized org {AnonymizedOrgId} without database implementation",
            AnonymizeOrganizationId(organizationId));

        await Task.CompletedTask;
        return null; // Forces 'no subscription found' flow
    }

    // Placeholder for database update - safe fallback for production
    private async Task UpdateSubscriptionStatus(Guid organizationId, string subscriptionId, string status, CancellationToken cancellationToken)
    {
        // TODO (issue #124): Implement actual database persistence.
        _logger.LogInformation(
            "Mock subscription status update: Anonymized org {AnonymizedOrgId}, Subscription {SubscriptionId} => {Status}",
            AnonymizeOrganizationId(organizationId), subscriptionId, status);

        await Task.CompletedTask;
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        _logger.LogInformation("Checkout session completed: {SessionId}", session.Id);
        
        // Extract organization ID from metadata
        if (session.Metadata.TryGetValue("organization_id", out var orgIdString) && 
            Guid.TryParse(orgIdString, out var organizationId))
        {
            // TODO: Create or update subscription in database
            _logger.LogInformation("Creating subscription for anonymized org {AnonymizedOrgId}", AnonymizeOrganizationId(organizationId));
        }
        
        await Task.CompletedTask;
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Invoice payment succeeded: {InvoiceId}", invoice.Id);
        
        // TODO: Update subscription status in database
        await Task.CompletedTask;
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = stripeEvent.Data.Object as StripeSubscription;
        if (subscription == null) return;

        _logger.LogInformation("Subscription deleted: {SubscriptionId}", subscription.Id);
        
        // TODO: Update subscription status in database
        await Task.CompletedTask;
    }

    private string GetStripePriceId(string planId)
    {
        return planId.ToLower() switch
        {
            "starter" => _configuration["Stripe:Plans:Starter:PriceId"] ?? "price_starter_default",
            "growth" => _configuration["Stripe:Plans:Growth:PriceId"] ?? "price_growth_default",
            _ => throw new ArgumentException($"Unknown plan ID: {planId}")
        };
    }
}

