using AutoDocOps.Domain.Enums;

namespace AutoDocOps.Application.Common.Interfaces;

public interface ISecretSourceProvider
{
    SecretSource WebhookSecretSource { get; }
}
