using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace AutoDocOps.Infrastructure.Services;

internal sealed class SecretSourceProvider : ISecretSourceProvider
{
    public SecretSource WebhookSecretSource { get; }

    public SecretSourceProvider(IConfiguration configuration)
    {
        var env = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
        var config = configuration["Stripe:WebhookSecret"];    
        WebhookSecretSource = string.IsNullOrWhiteSpace(env) && string.IsNullOrWhiteSpace(config)
            ? SecretSource.Missing
            : !string.IsNullOrWhiteSpace(env) ? SecretSource.Env : SecretSource.ConfigFile;
    }
}
