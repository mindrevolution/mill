using System.Diagnostics;

namespace Mill.Api.Services.Providers;

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

    protected async Task<string?> RunCommand(string command, string args, string? workingDir = null)
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
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output : null;
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
            var result = await RunCommand(command, "--version");
            return result != null;
        }
        catch
        {
            return false;
        }
    }
}
