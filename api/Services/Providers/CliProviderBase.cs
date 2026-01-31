using System.Diagnostics;

namespace MillApi.Services.Providers;

/// <summary>
/// Base class for CLI-based issue providers (gh, glab, etc.)
/// </summary>
public abstract class CliProviderBase
{
    protected readonly ILogger Logger;

    protected CliProviderBase(ILogger logger)
    {
        Logger = logger;
    }

    protected async Task<string?> RunCommand(string command, string args, string? workingDir = null, int timeoutMs = 10000)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true, // Prevent stdin prompts from blocking
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return null;

            // Close stdin immediately to prevent prompts
            process.StandardInput.Close();

            // Use Task.WhenAny for timeout (more reliable on Windows)
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var exitTask = process.WaitForExitAsync();
            var timeoutTask = Task.Delay(timeoutMs);

            var completedTask = await Task.WhenAny(Task.WhenAll(outputTask, exitTask), timeoutTask);

            if (completedTask == timeoutTask)
            {
                Logger.LogWarning("Command timed out after {Timeout}ms: {Command} {Args}", timeoutMs, command, args);
                try { process.Kill(entireProcessTree: true); } catch { }
                return null;
            }

            return process.ExitCode == 0 ? await outputTask : null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to run command: {Command} {Args}", command, args);
            return null;
        }
    }

    protected async Task<bool> IsCommandAvailable(string command)
    {
        try
        {
            // Quick check with short timeout
            var result = await RunCommand(command, "--version", timeoutMs: 3000);
            return result != null;
        }
        catch
        {
            return false;
        }
    }
}
