namespace AutoDocOps.Infrastructure.Constants;

/// <summary>
/// Constants used in testing scenarios to avoid magic numbers
/// </summary>
public static class TestConstants
{
    /// <summary>
    /// Overflow testing constants
    /// </summary>
    public static class Overflow
    {
        /// <summary>
        /// Small offset in ticks added to trigger overflow conditions in tests
        /// This ensures that when TimeSpan values are doubled, they exceed long.MaxValue
        /// </summary>
        public const long OverflowTriggerOffsetTicks = 1000;
        
        /// <summary>
        /// Timeout value that when doubled will cause an overflow
        /// </summary>
        public static readonly TimeSpan NearMaxTimeSpan = new(long.MaxValue / 2 + OverflowTriggerOffsetTicks);
    }
    
    /// <summary>
    /// Performance testing constants
    /// </summary>
    public static class Performance
    {
        /// <summary>
        /// Standard timeout for performance-critical operations (milliseconds)
        /// </summary>
        public const int StandardTimeoutMs = 5000;
        
        /// <summary>
        /// Maximum acceptable response time for API calls (milliseconds)
        /// </summary>
        public const int MaxApiResponseTimeMs = 2000;
        
        /// <summary>
        /// Default batch size for bulk operations
        /// </summary>
        public const int DefaultBatchSize = 100;
    }
    
    /// <summary>
    /// Memory testing constants
    /// </summary>
    public static class Memory
    {
        /// <summary>
        /// Maximum allowed memory allocation per request (bytes)
        /// </summary>
        public const int MaxRequestMemoryBytes = 256 * 1024; // 256 KB
        
        /// <summary>
        /// Buffer size for streaming operations (bytes)
        /// </summary>
        public const int StreamBufferSize = 4096; // 4 KB
        
        /// <summary>
        /// Threshold for using pooled buffers vs stack allocation
        /// </summary>
        public const int StackAllocThreshold = 1024; // 1 KB
    }
    
    /// <summary>
    /// Rate limiting test constants
    /// </summary>
    public static class RateLimit
    {
        /// <summary>
        /// Default number of requests allowed per time window
        /// </summary>
        public const int DefaultRequestLimit = 30;
        
        /// <summary>
        /// Standard time window for rate limiting (minutes)
        /// </summary>
        public const int StandardWindowMinutes = 1;
        
        /// <summary>
        /// Burst capacity for rate limiting
        /// </summary>
        public const int BurstCapacity = 10;
    }
    
    /// <summary>
    /// Database testing constants
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// Connection timeout for database operations (seconds)
        /// </summary>
        public const int ConnectionTimeoutSeconds = 30;
        
        /// <summary>
        /// Command timeout for database queries (seconds)
        /// </summary>
        public const int CommandTimeoutSeconds = 120;
        
        /// <summary>
        /// Maximum retry attempts for database operations
        /// </summary>
        public const int MaxRetryAttempts = 3;
    }
}