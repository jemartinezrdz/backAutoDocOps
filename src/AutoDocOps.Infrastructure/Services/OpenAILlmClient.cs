using AutoDocOps.Application.Common.Interfaces;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ClientModel;
using System.Runtime.CompilerServices;

namespace AutoDocOps.Infrastructure.Services;

public class OpenAILlmClient : ILlmClient
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAILlmClient> _logger;

    public OpenAILlmClient(IConfiguration configuration, ILogger<OpenAILlmClient> logger)
    {
        _logger = logger;
        
        var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OpenAI API key is not configured");

        // Basic API key validation: non-empty and plausible format (OpenAI keys typically start with "sk-")
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 20 || !apiKey.StartsWith("sk-"))
            throw new InvalidOperationException("OpenAI API key is invalid or malformed");
            
        var endpoint = configuration["OpenAI:Endpoint"] ?? Environment.GetEnvironmentVariable("OPENAI_API_BASE");
        var model = configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";

        if (!string.IsNullOrEmpty(endpoint))
        {
            // Use Azure OpenAI
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
            _chatClient = azureClient.GetChatClient(model);
        }
        else
        {
            // Use OpenAI directly
            var openAIClient = new OpenAI.OpenAIClient(apiKey);
            _chatClient = openAIClient.GetChatClient(model);
        }
    }

    public async IAsyncEnumerable<string> StreamChatAsync(string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("Eres un asistente útil especializado en documentación técnica y desarrollo de software."),
            new UserChatMessage(query)
        };

        var streamingResponse = _chatClient.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken);
        
        await foreach (var update in streamingResponse)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
                
            if (update.ContentUpdate.Count > 0)
            {
                foreach (var contentPart in update.ContentUpdate)
                {
                    if (!string.IsNullOrEmpty(contentPart.Text))
                    {
                        yield return contentPart.Text;
                    }
                }
            }
        }
    }

    public async Task<string> ChatAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("Eres un asistente útil especializado en documentación técnica y desarrollo de software."),
                new UserChatMessage(query)
            };

            var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat completion for query: {Query}", query);
            return $"Error: {ex.Message}";
        }
    }
}

