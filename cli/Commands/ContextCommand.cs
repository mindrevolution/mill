using Mill.Models;
using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Context commands for managing project context.md.
/// </summary>
public static class ContextCommand
{
    public static async Task<int> Run(string[] args, bool human)
    {
        if (args.Length == 0 || args[0] == "show")
        {
            return await Show(human);
        }

        return args[0] switch
        {
            "show" => await Show(human),
            "status" => Status(human),
            _ => ShowHelp()
        };
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill context [command]

            commands:
              show (default)        show context.md content
              status                show context freshness status
            """);
        return 1;
    }

    private static string ContextPath => Path.Combine(ProjectContext.MillFolder, "context.md");
    private static string ContextMetaPath => Path.Combine(ProjectContext.MillFolder, "context.meta.json");

    private static async Task<int> Show(bool human)
    {
        if (!File.Exists(ContextPath))
        {
            if (human)
            {
                Output.Warn("No context.md found. Run /mill:warmup to generate.");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new { exists = false }));
            }
            return 0;
        }

        var content = await File.ReadAllTextAsync(ContextPath);

        if (human)
        {
            Console.WriteLine(content);
        }
        else
        {
            var info = new FileInfo(ContextPath);
            Console.WriteLine(JsonHelper.Serialize(new
            {
                exists = true,
                path = ContextPath,
                content,
                updatedAt = info.LastWriteTimeUtc
            }));
        }

        return 0;
    }

    private const int StaleThreshold = 20; // commits before full regeneration needed

    private static int Status(bool human)
    {
        var exists = File.Exists(ContextPath);
        DateTime? updatedAt = null;
        string? commitHash = null;
        string? currentHash = null;
        int commitsBehind = 0;
        string freshness = "missing"; // missing | fresh | recent | stale

        if (exists)
        {
            var info = new FileInfo(ContextPath);
            updatedAt = info.LastWriteTimeUtc;
        }

        // Try to read commit hash from context.md first line
        if (exists)
        {
            try
            {
                using var reader = new StreamReader(ContextPath);
                var firstLine = reader.ReadLine();
                if (firstLine?.StartsWith("<!-- mill-context-hash:") == true)
                {
                    var start = firstLine.IndexOf(':') + 1;
                    var end = firstLine.IndexOf("-->");
                    if (start > 0 && end > start)
                    {
                        commitHash = firstLine[start..end].Trim();
                    }
                }
            }
            catch { }
        }

        // Get current HEAD
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", "rev-parse HEAD")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ProjectContext.ProjectPath
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                currentHash = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
            }
        }
        catch { }

        // Count commits between context hash and HEAD
        if (exists && commitHash != null && currentHash != null)
        {
            if (commitHash == currentHash)
            {
                freshness = "fresh";
                commitsBehind = 0;
            }
            else
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo("git", $"rev-list --count {commitHash}..HEAD")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = ProjectContext.ProjectPath
                    };
                    using var process = System.Diagnostics.Process.Start(psi);
                    if (process != null)
                    {
                        var countStr = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();
                        if (int.TryParse(countStr, out var count))
                        {
                            commitsBehind = count;
                            freshness = count <= StaleThreshold ? "recent" : "stale";
                        }
                    }
                }
                catch
                {
                    freshness = "stale"; // can't determine, assume stale
                }
            }
        }

        if (human)
        {
            if (!exists)
            {
                Output.Warn("Context not generated");
            }
            else if (freshness == "fresh")
            {
                Output.Success("Context ready");
                Output.Field("Commit", commitHash?[..8] ?? "unknown", dim: true);
            }
            else if (freshness == "recent")
            {
                Output.Success($"Context ready ({commitsBehind} new commits)");
                Output.Field("Context", commitHash?[..8] ?? "unknown", dim: true);
                Output.Field("Current", currentHash?[..8] ?? "unknown", dim: true);
            }
            else
            {
                Output.Warn($"Context stale ({commitsBehind} commits behind)");
                Output.Field("Context", commitHash?[..8] ?? "unknown", dim: true);
                Output.Field("Current", currentHash?[..8] ?? "unknown", dim: true);
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(new
            {
                exists,
                freshness,
                commitsBehind,
                path = exists ? ContextPath : null,
                updatedAt,
                commitHash,
                currentHash
            }));
        }

        return 0;
    }

    private record ContextMeta(string? CommitHash, DateTime? GeneratedAt);
}
