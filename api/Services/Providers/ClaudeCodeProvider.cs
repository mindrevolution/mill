using System.Diagnostics;
using System.Text;
using MillApi.Models;

namespace MillApi.Services.Providers;

/// <summary>
/// LLM provider that uses the Claude Code CLI.
/// </summary>
public class ClaudeCodeProvider : CliProviderBase, ILlmProvider
{
    private const string CommandName = "claude";

    public ClaudeCodeProvider(ILogger<ClaudeCodeProvider> logger) : base(logger)
    {
    }

    public string Name => "claude";

    public async Task<LlmResponse> Execute(string prompt, LlmOptions options)
    {
        var workingDir = options.WorkingDir ?? Directory.GetCurrentDirectory();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = CommandName,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Build arguments: claude --print "prompt"
            // Using --print for non-interactive mode
            psi.Arguments = $"--print \"{EscapeArgument(prompt)}\"";

            if (!string.IsNullOrEmpty(options.SystemPrompt))
            {
                psi.Arguments += $" --system \"{EscapeArgument(options.SystemPrompt)}\"";
            }

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new LlmResponse(false, "", "Failed to start Claude CLI process");
            }

            // Close stdin immediately
            process.StandardInput.Close();

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // Read output asynchronously
            var outputTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                {
                    outputBuilder.AppendLine(line);
                }
            });

            var errorTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await process.StandardError.ReadLineAsync()) != null)
                {
                    errorBuilder.AppendLine(line);
                }
            });

            var exitTask = process.WaitForExitAsync();
            var allTasks = Task.WhenAll(outputTask, errorTask, exitTask);

            if (options.TimeoutMs.HasValue)
            {
                var timeoutTask = Task.Delay(options.TimeoutMs.Value);
                var completedTask = await Task.WhenAny(allTasks, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Logger.LogWarning("Claude CLI timed out after {Timeout}ms", options.TimeoutMs);
                    try { process.Kill(entireProcessTree: true); } catch { }
                    return new LlmResponse(false, "", $"Command timed out after {options.TimeoutMs}ms");
                }
            }

            await allTasks;

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();

            if (process.ExitCode != 0)
            {
                Logger.LogWarning("Claude CLI exited with code {ExitCode}: {Error}", process.ExitCode, error);
                return new LlmResponse(false, output, error);
            }

            return new LlmResponse(true, output, null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to execute Claude CLI");
            return new LlmResponse(false, "", ex.Message);
        }
    }

    public Process SpawnInteractive(string prompt, string? workingDir = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = CommandName,
            Arguments = $"\"{EscapeArgument(prompt)}\"",
            WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            CreateNoWindow = false
        };

        return Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start Claude CLI process");
    }

    public async Task<bool> IsAvailable()
    {
        return await IsCommandAvailable(CommandName);
    }

    private static string EscapeArgument(string arg)
    {
        // Escape double quotes and backslashes for command line
        return arg
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}
