using AutoDocOps.Application.Common.Interfaces;
using Stripe;

namespace AutoDocOps.Infrastructure.Services;

public sealed class NullBillingService : IBillingService
{
    public Task HandleAsync(Event stripeEvent, CancellationToken cancellationToken = default)
    {
        // No-op implementation for testing
        return Task.CompletedTask;
    }

    public Task<string> CreateCheckoutSessionAsync(Guid organizationId, string planId, string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        // Return a dummy session ID for testing
        return Task.FromResult("cs_test_dummy_session_id");
    }

    public Task<bool> CancelSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        // Return true for testing
        return Task.FromResult(true);
    }
}