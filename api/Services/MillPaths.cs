namespace MillApi.Services;

/// <summary>
/// Central resolver for mill installation paths.
/// Resolved once at startup and available globally.
/// </summary>
public class MillPaths
{
    /// <summary>
    /// Root directory of the mill installation.
    /// </summary>
    public string Home { get; }

    /// <summary>
    /// Path to ground workspace assets (prompts, templates).
    /// </summary>
    public string Ground => Path.Combine(Home, "ground");

    /// <summary>
    /// Path to ground templates (archetypes, stacks).
    /// </summary>
    public string Templates => Path.Combine(Ground, "templates");

    /// <summary>
    /// Path to ground prompts.
    /// </summary>
    public string GroundPrompts => Path.Combine(Ground, "prompts");

    /// <summary>
    /// Path to shape workspace assets.
    /// </summary>
    public string Shape => Path.Combine(Home, "shape");

    /// <summary>
    /// Path to shape prompts.
    /// </summary>
    public string ShapePrompts => Path.Combine(Shape, "prompts");

    /// <summary>
    /// Path to ship workspace assets.
    /// </summary>
    public string Ship => Path.Combine(Home, "ship");

    public MillPaths(IConfiguration config)
    {
        Home = FindMillHome(config["Mill:Home"]);
    }

    private static string FindMillHome(string? configOverride)
    {
        // 1. Explicit config override
        if (!string.IsNullOrEmpty(configOverride) && Directory.Exists(configOverride))
            return configOverride;

        // 2. MILL_HOME environment variable
        var env = Environment.GetEnvironmentVariable("MILL_HOME");
        if (!string.IsNullOrEmpty(env) && IsMillRoot(env))
            return env;

        // 3. Walk up from executable to find mill root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (IsMillRoot(dir.FullName))
                return dir.FullName;
            dir = dir.Parent;
        }

        // 4. Fallback (will likely fail, but provides useful error path)
        return AppContext.BaseDirectory;
    }

    private static bool IsMillRoot(string path)
    {
        // Mill root has ground/templates directory
        return Directory.Exists(Path.Combine(path, "ground", "templates"));
    }
}
