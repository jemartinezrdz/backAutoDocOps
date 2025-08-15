namespace AutoDocOps.WebAPI.Options;

public sealed class WebhookLimitsOptions
{
    public int MaxBytes { get; set; } = 256 * 1024;
}