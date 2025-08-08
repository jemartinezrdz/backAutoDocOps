namespace AutoDocOps.Infrastructure.Services;

public class DocumentationGenerationOptions
{
    public const string SectionName = "DocumentationGeneration";

    /// <summary>
    /// Interval in seconds between checking for pending passports
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Delay in seconds between each simulation phase (for development/testing)
    /// </summary>
    public int SimulationPhaseDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Whether to enable simulation mode (for development/testing)
    /// </summary>
    public bool EnableSimulation { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent documentation generations
    /// </summary>
    public int MaxConcurrentGenerations { get; set; } = 5;

    /// <summary>
    /// Retry delay in minutes when an error occurs
    /// </summary>
    public int RetryDelayMinutes { get; set; } = 1;
}
