using System.Buffers;
using System.Text;
using System.Globalization;

namespace AutoDocOps.Infrastructure.Helpers;

/// <summary>
/// Utility class for efficient memory management operations
/// </summary>
public static class MemoryHelper
{
    private const int DefaultBufferSize = 4096;
    private const int MaxStackAlloc = 1024;
    
    /// <summary>
    /// Reads a stream into a string using pooled buffers for memory efficiency
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="maxBytes">Maximum bytes to read (prevents memory exhaustion)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The string content of the stream</returns>
    public static async Task<string> ReadStreamToStringAsync(
        Stream stream,
        int maxBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        
        var rentedBuffer = ArrayPool<byte>.Shared.Rent(Math.Min(maxBytes, DefaultBufferSize));
        try
        {
            using var memoryStream = new MemoryStream();
            int totalRead = 0;
            int bytesRead;
            
            while (totalRead < maxBytes && 
                   (bytesRead = await stream.ReadAsync(
                       rentedBuffer.AsMemory(0, Math.Min(rentedBuffer.Length, maxBytes - totalRead)), 
                       cancellationToken).ConfigureAwait(false)) > 0)
            {
                totalRead += bytesRead;
                await memoryStream.WriteAsync(rentedBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                
                // Check if we've hit the limit
                if (totalRead >= maxBytes)
                {
                    break;
                }
            }
            
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }
    
    /// <summary>
    /// Reads a fixed amount from a stream using pooled memory
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="maxBytes">Maximum bytes to read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (data as string, actualBytesRead, hasMoreData)</returns>
    public static async Task<(string Data, int BytesRead, bool HasMoreData)> ReadStreamWithLimitAsync(
        Stream stream,
        int maxBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        
        var rentedBuffer = ArrayPool<byte>.Shared.Rent(maxBytes);
        try
        {
            int totalRead = 0;
            int bytesRead;
            var bufferMemory = rentedBuffer.AsMemory();
            
            while (totalRead < maxBytes && 
                   (bytesRead = await stream.ReadAsync(bufferMemory.Slice(totalRead, maxBytes - totalRead), cancellationToken).ConfigureAwait(false)) > 0)
            {
                totalRead += bytesRead;
            }
            
            // Check if there's more data available
            bool hasMoreData = false;
            if (totalRead == maxBytes)
            {
                var testBuffer = ArrayPool<byte>.Shared.Rent(1);
                try
                {
                    hasMoreData = await stream.ReadAsync(testBuffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false) > 0;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(testBuffer);
                }
            }
            
            var data = Encoding.UTF8.GetString(rentedBuffer, 0, totalRead);
            return (data, totalRead, hasMoreData);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }
    
    /// <summary>
    /// Copies data between streams using pooled buffers
    /// </summary>
    /// <param name="source">Source stream</param>
    /// <param name="destination">Destination stream</param>
    /// <param name="maxBytes">Maximum bytes to copy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of bytes copied</returns>
    public static async Task<long> CopyStreamAsync(
        Stream source,
        Stream destination,
        long maxBytes = long.MaxValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        
        var rentedBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        try
        {
            long totalCopied = 0;
            int bytesRead;
            
            while (totalCopied < maxBytes &&
                   (bytesRead = await source.ReadAsync(
                       rentedBuffer.AsMemory(0, (int)Math.Min(rentedBuffer.Length, maxBytes - totalCopied)),
                       cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(rentedBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalCopied += bytesRead;
            }
            
            return totalCopied;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }
    
    /// <summary>
    /// Efficiently formats large strings using pooled arrays when needed
    /// </summary>
    /// <param name="format">Format string</param>
    /// <param name="args">Arguments</param>
    /// <returns>Formatted string</returns>
    public static string FormatLarge(string format, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(format);
        
        // For small strings, use normal formatting
        var estimatedSize = format.Length + (args?.Sum(a => a?.ToString()?.Length ?? 0) ?? 0);
        if (estimatedSize <= MaxStackAlloc)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args ?? Array.Empty<object>());
        }
        
        // For larger strings, use StringBuilder with appropriate capacity
        var sb = new StringBuilder(estimatedSize);
        sb.AppendFormat(CultureInfo.InvariantCulture, format, args ?? Array.Empty<object>());
        return sb.ToString();
    }
}