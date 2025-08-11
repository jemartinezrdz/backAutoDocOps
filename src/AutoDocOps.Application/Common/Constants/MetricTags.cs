namespace AutoDocOps.Application.Common.Constants;

/// <summary>
/// Centralized metric tag keys and values to prevent typos and control cardinality.
/// </summary>
public static class MetricTags
{
    public const string Result = "result";
    public const string Reason = "reason";
    public const string Op = "op";

    public const string Ok = "ok";
    public const string Fail = "fail";

    public static class Reasons
    {
        public const string Signature = "signature";
        public const string TooLarge = "too_large";
        public const string UnsupportedMedia = "unsupported_media";
        public const string Empty = "empty";
        public const string Timeout = "timeout";
        public const string Other = "other";
    }

    public static class BillingOps
    {
        public const string CreateCheckout = "create_checkout";
        public const string CancelSubscription = "cancel_subscription";
        public const string HandleEvent = "handle_event";
    }
}
