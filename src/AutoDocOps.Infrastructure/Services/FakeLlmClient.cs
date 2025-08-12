using AutoDocOps.Application.Common.Interfaces;
using System.Runtime.CompilerServices;

namespace AutoDocOps.Infrastructure.Services;

public class FakeLlmClient : ILlmClient
{
    public async IAsyncEnumerable<string> StreamChatAsync(string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = $"Esta es una respuesta simulada para la consulta: '{query}'. ";
        var words = response.Split(' ');
        
        foreach (var word in words)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
                
            yield return word + " ";
            await Task.Delay(100, cancellationToken).ConfigureAwait(false); // Simular delay de streaming
        }
        
        yield return "\n\nEsta es una respuesta generada por el cliente LLM falso para propósitos de testing.";
    }

    public async Task<string> ChatAsync(string query, CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken).ConfigureAwait(false); // Simular procesamiento
        
        return $"Respuesta simulada para: '{query}'. Esta es una respuesta generada por el cliente LLM falso para propósitos de testing.";
    }
}

