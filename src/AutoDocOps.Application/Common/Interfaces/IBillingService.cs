using Stripe;

namespace AutoDocOps.Application.Common.Interfaces;

public interface IBillingService
{
    Task HandleAsync(Event stripeEvent, CancellationToken cancellationToken = default);
    Task<string> CreateCheckoutSessionAsync(Guid organizationId, string planId, string successUrl, string cancelUrl, CancellationToken cancellationToken = default);
    Task<bool> CancelSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

