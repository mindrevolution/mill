using System.Diagnostics;

namespace Mill.Services;

/// <summary>
/// Provides project path resolution and .mill folder access.
/// </summary>
public static class ProjectContext
{
    /// <summary>
    /// Get the project root (git root or current directory).
    /// </summary>
    public static string ProjectPath => GetGitRoot() ?? Directory.GetCurrentDirectory();

    /// <summary>
    /// Get path to .mill folder in project.
    /// </summary>
    public static string MillFolder => Path.Combine(ProjectPath, ".mill");

    /// <summary>
    /// Check if .mill folder exists.
    /// </summary>
    public static bool IsInitialized => Directory.Exists(MillFolder);

    /// <summary>
    /// Get the git root directory, or null if not in a git repo.
    /// </summary>
    public static string? GetGitRoot()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --show-toplevel",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Ensure .mill folder exists.
    /// </summary>
    public static void EnsureMillFolder()
    {
        Directory.CreateDirectory(MillFolder);
    }
}
