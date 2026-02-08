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

    private static int Status(bool human)
    {
        var exists = File.Exists(ContextPath);
        DateTime? updatedAt = null;
        string? commitHash = null;

        if (exists)
        {
            var info = new FileInfo(ContextPath);
            updatedAt = info.LastWriteTimeUtc;
        }

        // Try to read meta file for commit hash
        if (File.Exists(ContextMetaPath))
        {
            try
            {
                var metaJson = File.ReadAllText(ContextMetaPath);
                var meta = JsonHelper.Deserialize<ContextMeta>(metaJson);
                commitHash = meta?.CommitHash;
            }
            catch { }
        }

        if (human)
        {
            if (!exists)
            {
                Output.Warn("Context not generated");
            }
            else
            {
                Output.Success("Context ready");
                Output.Field("Path", ContextPath);
                Output.Field("Updated", updatedAt?.ToString("yyyy-MM-dd HH:mm"));
                if (commitHash != null)
                {
                    Output.Field("Commit", commitHash[..8], dim: true);
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(new
            {
                exists,
                path = exists ? ContextPath : null,
                updatedAt,
                commitHash
            }));
        }

        return 0;
    }

    private record ContextMeta(string? CommitHash, DateTime? GeneratedAt);
}
