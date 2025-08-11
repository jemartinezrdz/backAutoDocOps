using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using StripeSubscription = Stripe.Subscription;
using System.Security.Cryptography;
using System.Text;
using AutoDocOps.Infrastructure.Logging;
using AutoDocOps.Infrastructure.Monitoring;
using AutoDocOps.Application.Common.Constants;
using System.Diagnostics;

namespace AutoDocOps.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly ILogger<BillingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly StripeClient _stripeClient;

    public BillingService(ILogger<BillingService> logger, IConfiguration configuration, StripeClient stripeClient)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(stripeClient);
        _logger = logger;
        _configuration = configuration;
        _stripeClient = stripeClient;
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
        ArgumentNullException.ThrowIfNull(stripeEvent);
        var sw = Stopwatch.StartNew();
        var result = MetricTags.Ok;
        try
        {
            _logger.ProcessingStripeEvent(stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
            await HandleCheckoutSessionCompleted(stripeEvent, cancellationToken).ConfigureAwait(false);
                    break;
                
                case "invoice.payment_succeeded":
            await HandleInvoicePaymentSucceeded(stripeEvent, cancellationToken).ConfigureAwait(false);
                    break;
                
                case "customer.subscription.deleted":
            await HandleSubscriptionDeleted(stripeEvent, cancellationToken).ConfigureAwait(false);
                    break;
                
                default:
            _logger.UnhandledStripeEventType(stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            result = MetricTags.Fail;
            _logger.ErrorProcessingStripeEventDetailed(stripeEvent.Type, stripeEvent.Id, ex);
            throw;
        }
        finally
        {
            sw.Stop();
            BillingMetrics.Operations.Add(1, new KeyValuePair<string, object?>(MetricTags.Op, MetricTags.BillingOps.HandleEvent), new KeyValuePair<string, object?>(MetricTags.Result, result));
            BillingMetrics.OperationLatencySeconds.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>(MetricTags.Op, MetricTags.BillingOps.HandleEvent), new KeyValuePair<string, object?>(MetricTags.Result, result));
        }
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid organizationId, string planId, 
        string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(planId);
        ArgumentNullException.ThrowIfNull(successUrl);
        ArgumentNullException.ThrowIfNull(cancelUrl);
        var sw = Stopwatch.StartNew();
        var result = MetricTags.Ok;
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

            var service = new SessionService(_stripeClient);
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken).ConfigureAwait(false);
            
            _logger.CreatedCheckoutSession(session.Id, AnonymizeOrganizationId(organizationId));
            
            return session.Url;
        }
        catch (Exception ex)
        {
            result = MetricTags.Fail;
            _logger.ErrorCreatingCheckoutSession(AnonymizeOrganizationId(organizationId), ex);
            throw;
        }
        finally
        {
            sw.Stop();
            BillingMetrics.Operations.Add(1, new KeyValuePair<string, object?>(MetricTags.Op, MetricTags.BillingOps.CreateCheckout), new KeyValuePair<string, object?>(MetricTags.Result, result));
            BillingMetrics.OperationLatencySeconds.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>(MetricTags.Op, MetricTags.BillingOps.CreateCheckout), new KeyValuePair<string, object?>(MetricTags.Result, result));
        }
    }

    public async Task<bool> CancelSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = MetricTags.Ok;
        try
        {
            // TODO: Lookup subscription in database for the given organization
            // Placeholder: Assume we have a method GetSubscriptionIdForOrganization
            string? subscriptionId = await GetSubscriptionIdForOrganization(organizationId, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.NoSubscriptionFound(AnonymizeOrganizationId(organizationId));
                return false;
            }

            var subscriptionService = new SubscriptionService(_stripeClient);
            var canceledSubscription = await subscriptionService.CancelAsync(subscriptionId, null, cancellationToken: cancellationToken).ConfigureAwait(false);

            // TODO: Update subscription status in database
            await UpdateSubscriptionStatus(organizationId, canceledSubscription.Id, "canceled", cancellationToken).ConfigureAwait(false);

            _logger.CanceledSubscription(canceledSubscription.Id, AnonymizeOrganizationId(organizationId));
            return true;
        }
        catch (Exception ex)
        {
            result = MetricTags.Fail;
            _logger.ErrorCancelingSubscription(AnonymizeOrganizationId(organizationId), ex);
            return false;
        }
        finally
        {
            sw.Stop();
            BillingMetrics.Operations.Add(1, new KeyValuePair<string, object?>(MetricTags.Op, MetricTags.BillingOps.CancelSubscription), new KeyValuePair<string, object?>(MetricTags.Result, result));
            BillingMetrics.OperationLatencySeconds.Record(sw.Elapsed.TotalSeconds, new KeyValuePair<string, object?>(MetricTags.Op, MetricTags.BillingOps.CancelSubscription), new KeyValuePair<string, object?>(MetricTags.Result, result));
        }
    }

    // Placeholder for database lookup - safe fallback for production
    private async Task<string?> GetSubscriptionIdForOrganization(Guid organizationId, CancellationToken cancellationToken)
    {
        // TODO (issue #123): Replace with actual repository query.
        _logger.GetSubscriptionIdPlaceholder(AnonymizeOrganizationId(organizationId));

    await Task.CompletedTask.ConfigureAwait(false);
        return null; // Forces 'no subscription found' flow
    }

    // Placeholder for database update - safe fallback for production
    private async Task UpdateSubscriptionStatus(Guid organizationId, string subscriptionId, string status, CancellationToken cancellationToken)
    {
        // TODO (issue #124): Implement actual database persistence.
        _logger.MockSubscriptionStatusUpdate(AnonymizeOrganizationId(organizationId), subscriptionId, status);

    await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            return;
        }

        _logger.CheckoutSessionCompleted(session.Id);
        
        // Extract organization ID from metadata
        if (session.Metadata.TryGetValue("organization_id", out var orgIdString) && 
            Guid.TryParse(orgIdString, out var organizationId))
        {
            // TODO: Create or update subscription in database
            _logger.CreatingSubscriptionForOrg(AnonymizeOrganizationId(organizationId));
        }
        
    await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
        {
            return;
        }

        _logger.InvoicePaymentSucceeded(invoice.Id);
        
        // TODO: Update subscription status in database
    await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = stripeEvent.Data.Object as StripeSubscription;
        if (subscription == null)
        {
            return;
        }

        _logger.SubscriptionDeleted(subscription.Id);
        
        // TODO: Update subscription status in database
    await Task.CompletedTask.ConfigureAwait(false);
    }

    private string GetStripePriceId(string planId)
    {
        return planId.ToLowerInvariant() switch
        {
            "starter" => _configuration["Stripe:Plans:Starter:PriceId"] ?? "price_starter_default",
            "growth" => _configuration["Stripe:Plans:Growth:PriceId"] ?? "price_growth_default",
            _ => throw new ArgumentException($"Unknown plan ID: {planId}")
        };
    }
}

