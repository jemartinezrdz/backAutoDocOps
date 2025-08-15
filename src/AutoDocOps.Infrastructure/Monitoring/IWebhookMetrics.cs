namespace AutoDocOps.Infrastructure.Monitoring;

public interface IWebhookMetrics
{
    void ObserveRequest(string source);
    void ObserveInvalid(string source, string reason);
    void ObserveProcessed(string source, string outcome);
}