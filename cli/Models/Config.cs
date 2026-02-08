namespace Mill.Models;

public record ProjectConfig
{
    public List<string> Exclude { get; init; } = ["backlog", "deferred", "on-hold", "wontfix", "duplicate", "invalid"];
    public ContextConfig Context { get; init; } = new();
}

public record ContextConfig
{
    /// <summary>
    /// Number of commits before context is considered stale.
    /// </summary>
    public int StaleThreshold { get; init; } = 25;
}
