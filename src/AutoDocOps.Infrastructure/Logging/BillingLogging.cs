using Microsoft.Extensions.Logging;

namespace AutoDocOps.Infrastructure.Logging;

internal static partial class BillingLog
{
    [LoggerMessage(EventId = 3100, Level = LogLevel.Information, Message = "Processing Stripe event: {EventType} with ID: {EventId}")]
    internal static partial void ProcessingStripeEvent(this ILogger logger, string eventType, string eventId);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Information, Message = "Unhandled Stripe event type: {EventType}")]
    internal static partial void UnhandledStripeEventType(this ILogger logger, string eventType);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Information, Message = "Created checkout session {SessionId} for anonymized org {Org}")]
    internal static partial void CreatedCheckoutSession(this ILogger logger, string sessionId, string org);

    [LoggerMessage(EventId = 3103, Level = LogLevel.Error, Message = "Error creating checkout session for anonymized org {Org}")]
    internal static partial void ErrorCreatingCheckoutSession(this ILogger logger, string org, Exception ex);

    [LoggerMessage(EventId = 3104, Level = LogLevel.Warning, Message = "No subscription found for anonymized org {Org}")]
    internal static partial void NoSubscriptionFound(this ILogger logger, string org);

    [LoggerMessage(EventId = 3105, Level = LogLevel.Information, Message = "Canceled subscription {SubscriptionId} for anonymized org {Org}")]
    internal static partial void CanceledSubscription(this ILogger logger, string subscriptionId, string org);

    [LoggerMessage(EventId = 3106, Level = LogLevel.Error, Message = "Error canceling subscription for anonymized org {Org}")]
    internal static partial void ErrorCancelingSubscription(this ILogger logger, string org, Exception ex);

    [LoggerMessage(EventId = 3107, Level = LogLevel.Warning, Message = "GetSubscriptionIdForOrganization placeholder invoked for anonymized org {Org}")]
    internal static partial void GetSubscriptionIdPlaceholder(this ILogger logger, string org);

    [LoggerMessage(EventId = 3108, Level = LogLevel.Information, Message = "Mock subscription status update: anonymized org {Org}, subscription {SubscriptionId} => {Status}")]
    internal static partial void MockSubscriptionStatusUpdate(this ILogger logger, string org, string subscriptionId, string status);

    [LoggerMessage(EventId = 3109, Level = LogLevel.Information, Message = "Checkout session completed: {SessionId}")]
    internal static partial void CheckoutSessionCompleted(this ILogger logger, string sessionId);

    [LoggerMessage(EventId = 3110, Level = LogLevel.Information, Message = "Creating subscription for anonymized org {Org}")]
    internal static partial void CreatingSubscriptionForOrg(this ILogger logger, string org);

    [LoggerMessage(EventId = 3111, Level = LogLevel.Information, Message = "Invoice payment succeeded: {InvoiceId}")]
    internal static partial void InvoicePaymentSucceeded(this ILogger logger, string invoiceId);

    [LoggerMessage(EventId = 3112, Level = LogLevel.Information, Message = "Subscription deleted: {SubscriptionId}")]
    internal static partial void SubscriptionDeleted(this ILogger logger, string subscriptionId);

    [LoggerMessage(EventId = 3113, Level = LogLevel.Error, Message = "Error processing Stripe event: {EventType} with ID: {EventId}")]
    internal static partial void ErrorProcessingStripeEventDetailed(this ILogger logger, string eventType, string eventId, Exception ex);
}
