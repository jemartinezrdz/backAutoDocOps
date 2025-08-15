namespace AutoDocOps.WebAPI.Options;

public sealed class RateLimitOptions
{
    public int WebhookPerMinute { get; set; } = 60;
}