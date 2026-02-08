namespace Mill.Services;

/// <summary>
/// Central resolver for mill installation paths.
/// </summary>
public static class MillPaths
{
    private static string? _home;

    /// <summary>
    /// Root directory of the mill installation.
    /// </summary>
    public static string Home => _home ??= FindMillHome();

    /// <summary>
    /// Path to templates directory.
    /// </summary>
    public static string Templates => Path.Combine(Home, "templates");

    /// <summary>
    /// Path to prompts directory.
    /// </summary>
    public static string Prompts => Path.Combine(Home, "prompts");

    /// <summary>
    /// Path to skills directory.
    /// </summary>
    public static string Skills => Path.Combine(Home, "skills");

    /// <summary>
    /// Path to archetypes templates.
    /// </summary>
    public static string Archetypes => Path.Combine(Templates, "archetypes");

    /// <summary>
    /// Path to stacks templates.
    /// </summary>
    public static string Stacks => Path.Combine(Templates, "stacks");

    /// <summary>
    /// Path to spec templates.
    /// </summary>
    public static string Specs => Path.Combine(Templates, "specs");

    /// <summary>
    /// Path to domain execution guides.
    /// </summary>
    public static string Domains => Path.Combine(Templates, "domains");

    private static string FindMillHome()
    {
        // 1. MILL_HOME environment variable
        var env = Environment.GetEnvironmentVariable("MILL_HOME");
        if (!string.IsNullOrEmpty(env) && IsMillRoot(env))
            return env;

        // 2. Walk up from executable to find mill root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (IsMillRoot(dir.FullName))
                return dir.FullName;
            dir = dir.Parent;
        }

        // 3. Fallback to executable directory
        return AppContext.BaseDirectory;
    }

    private static bool IsMillRoot(string path)
    {
        // Mill root has ground/templates directory
        return Directory.Exists(Path.Combine(path, "ground", "templates"));
    }
}
