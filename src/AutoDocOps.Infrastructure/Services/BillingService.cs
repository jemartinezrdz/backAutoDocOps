using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using StripeSubscription = Stripe.Subscription;

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
            
            _logger.LogInformation("Created checkout session {SessionId} for organization {OrganizationId}", 
                session.Id, organizationId);
            
            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for organization {OrganizationId}", organizationId);
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
                _logger.LogWarning("No subscription found for organization {OrganizationId}", organizationId);
                return false;
            }

            var subscriptionService = new SubscriptionService(new StripeClient(_stripeApiKey));
            var canceledSubscription = await subscriptionService.CancelAsync(subscriptionId, null, cancellationToken: cancellationToken);

            // TODO: Update subscription status in database
            await UpdateSubscriptionStatus(organizationId, canceledSubscription.Id, "canceled", cancellationToken);

            _logger.LogInformation("Canceled subscription {SubscriptionId} for organization {OrganizationId}", canceledSubscription.Id, organizationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription for organization {OrganizationId}", organizationId);
            return false;
        }
    }

    // Placeholder for database lookup
    private Task<string?> GetSubscriptionIdForOrganization(Guid organizationId, CancellationToken cancellationToken)
    {
        // This method must be implemented to perform a database lookup.
        throw new NotImplementedException("GetSubscriptionIdForOrganization must be implemented.");
    }

    // Placeholder for database update
    private Task UpdateSubscriptionStatus(Guid organizationId, string subscriptionId, string status, CancellationToken cancellationToken)
    {
        // This method must be implemented to perform a database update.
        throw new NotImplementedException("UpdateSubscriptionStatus must be implemented.");
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
            _logger.LogInformation("Creating subscription for organization {OrganizationId}", organizationId);
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

