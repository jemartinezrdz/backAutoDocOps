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

    public BillingService(ILogger<BillingService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Configure Stripe API key
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
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

            var service = new SessionService();
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
            // This would typically involve looking up the subscription in the database
            // and then canceling it via Stripe API
            _logger.LogInformation("Canceling subscription for organization {OrganizationId}", organizationId);
            
            // TODO: Implement actual subscription cancellation logic
            // 1. Look up subscription in database
            // 2. Cancel via Stripe API
            // 3. Update database
            
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription for organization {OrganizationId}", organizationId);
            return false;
        }
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

