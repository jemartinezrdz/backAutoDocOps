namespace AutoDocOps.Application.Common.Interfaces;

public interface ILlmClient
{
    IAsyncEnumerable<string> StreamChatAsync(string query, CancellationToken cancellationToken = default);
    Task<string> ChatAsync(string query, CancellationToken cancellationToken = default);
}

