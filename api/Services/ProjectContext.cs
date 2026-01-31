namespace MillApi.Services;

/// <summary>
/// Shared context for the current project path.
/// Singleton that holds the active project directory.
/// </summary>
public class ProjectContext
{
    private string? _projectPath;

    public ProjectContext(IConfiguration config)
    {
        _projectPath = config["Mill:ProjectPath"];
    }

    public void SetProjectPath(string path) => _projectPath = path;
    public string? GetProjectPath() => _projectPath;

    /// <summary>
    /// Get the project path or current directory as fallback.
    /// </summary>
    public string ProjectPath => _projectPath ?? Directory.GetCurrentDirectory();

    /// <summary>
    /// Get path to .mill folder in project.
    /// </summary>
    public string MillFolder => Path.Combine(ProjectPath, ".mill");
}
