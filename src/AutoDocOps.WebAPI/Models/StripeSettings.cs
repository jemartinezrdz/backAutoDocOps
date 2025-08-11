namespace AutoDocOps.WebAPI.Models;

public sealed record StripeSettings
{
    public string? WebhookSecret { get; init; }
}
