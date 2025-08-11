using Microsoft.Extensions.Logging;
using System;

namespace AutoDocOps.WebAPI.Controllers
{
    internal static partial class Log
    {
        [LoggerMessage(
            EventId = 1001,
            Level = LogLevel.Information,
            Message = "Retrieved {Count} projects for organization {OrganizationId}")]
        internal static partial void RetrievedProjects(this ILogger logger, int count, Guid organizationId);

        [LoggerMessage(
            EventId = 1002,
            Level = LogLevel.Error,
            Message = "Error retrieving projects for organization {OrganizationId}")]
        internal static partial void ErrorRetrievingProjects(this ILogger logger, Guid organizationId, Exception ex);

        [LoggerMessage(
            EventId = 1003,
            Level = LogLevel.Information,
            Message = "Retrieving project {ProjectId}")]
        internal static partial void RetrievingProject(this ILogger logger, Guid projectId);

        [LoggerMessage(
            EventId = 1004,
            Level = LogLevel.Warning,
            Message = "Project {ProjectId} not found")]
        internal static partial void ProjectNotFound(this ILogger logger, Guid projectId, Exception ex);

        [LoggerMessage(
            EventId = 1005,
            Level = LogLevel.Error,
            Message = "Error retrieving project {ProjectId}")]
        internal static partial void ErrorRetrievingProject(this ILogger logger, Guid projectId, Exception ex);

        [LoggerMessage(
            EventId = 1006,
            Level = LogLevel.Information,
            Message = "Created project {ProjectId} for organization {OrganizationId}")]
        internal static partial void CreatedProject(this ILogger logger, Guid projectId, Guid organizationId);

        [LoggerMessage(
            EventId = 1007,
            Level = LogLevel.Warning,
            Message = "Invalid project creation request")]
        internal static partial void InvalidProjectCreationRequest(this ILogger logger, Exception ex);

        [LoggerMessage(
            EventId = 1008,
            Level = LogLevel.Error,
            Message = "Error creating project")]
        internal static partial void ErrorCreatingProject(this ILogger logger, Exception ex);

        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "Retrieving passport {PassportId}")]
        internal static partial void RetrievingPassport(this ILogger logger, Guid passportId);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Warning,
            Message = "Passport {PassportId} not found")]
        internal static partial void PassportNotFound(this ILogger logger, Guid passportId, Exception ex);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Error,
            Message = "Error retrieving passport {PassportId}")]
        internal static partial void ErrorRetrievingPassport(this ILogger logger, Guid passportId, Exception ex);

        [LoggerMessage(
            EventId = 2004,
            Level = LogLevel.Information,
            Message = "Retrieving passports for project {ProjectId}")]
        internal static partial void RetrievingPassportsForProject(this ILogger logger, Guid projectId);

        [LoggerMessage(
            EventId = 2005,
            Level = LogLevel.Error,
            Message = "Error retrieving passports for project {ProjectId}")]
        internal static partial void ErrorRetrievingPassportsForProject(this ILogger logger, Guid projectId, Exception ex);

        [LoggerMessage(
            EventId = 2006,
            Level = LogLevel.Information,
            Message = "Deleting passport {PassportId}")]
        internal static partial void DeletingPassport(this ILogger logger, Guid passportId);

        [LoggerMessage(
            EventId = 2007,
            Level = LogLevel.Error,
            Message = "Error deleting passport {PassportId}")]
        internal static partial void ErrorDeletingPassport(this ILogger logger, Guid passportId, Exception ex);

        [LoggerMessage(
            EventId = 2008,
            Level = LogLevel.Information,
            Message = "Downloading passport {PassportId} in format {Format}")]
        internal static partial void DownloadingPassport(this ILogger logger, Guid passportId, string format);
        
        [LoggerMessage(
            EventId = 2009,
            Level = LogLevel.Error,
            Message = "Error downloading passport {PassportId}")]
        internal static partial void ErrorDownloadingPassport(this ILogger logger, Guid passportId, Exception ex);

        [LoggerMessage(
            EventId = 3000,
            Level = LogLevel.Error,
            Message = "STRIPE_WEBHOOK_SECRET environment variable is not configured")]
        internal static partial void StripeWebhookSecretNotConfigured(this ILogger logger);

        [LoggerMessage(
            EventId = 3001,
            Level = LogLevel.Warning,
            Message = "Invalid Stripe webhook signature")]
        internal static partial void InvalidStripeWebhookSignature(this ILogger logger, Exception ex);

        [LoggerMessage(
            EventId = 3002,
            Level = LogLevel.Error,
            Message = "Error processing Stripe webhook: {EventType}")]
        internal static partial void ErrorProcessingStripeWebhook(this ILogger logger, string eventType, Exception ex);

        [LoggerMessage(
            EventId = 3003,
            Level = LogLevel.Error,
            Message = "Chat streaming error: {Message}")]
        internal static partial void ChatStreamingError(this ILogger logger, string message, Exception ex);

        [LoggerMessage(
            EventId = 3004,
            Level = LogLevel.Information,
            Message = "AutoDocOps API starting in {Environment} environment")]
        internal static partial void ApiStarting(this ILogger logger, string environment);

        [LoggerMessage(
            EventId = 3005,
            Level = LogLevel.Information,
            Message = "Available endpoints - Swagger UI: /swagger, Health checks: /health")]
        internal static partial void AvailableEndpoints(this ILogger logger);

        [LoggerMessage(
            EventId = 3006,
            Level = LogLevel.Warning,
            Message = "Using config file for Stripe secret instead of environment variable in Production.")]
        internal static partial void UsingConfigStripeSecret(this ILogger logger);

        [LoggerMessage(
            EventId = 3007,
            Level = LogLevel.Information,
            Message = "Audit log stored for Stripe event {EventType} with secret source {SecretSource}")]
        internal static partial void StripeAuditLogged(this ILogger logger, string eventType, string secretSource);

        [LoggerMessage(
            EventId = 3008,
            Level = LogLevel.Warning,
            Message = "Audit log failed for Stripe event {EventType}: {Reason}")]
        internal static partial void StripeAuditLogFailed(this ILogger logger, string eventType, string reason);

    }
}