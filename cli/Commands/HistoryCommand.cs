using System.Diagnostics;
using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// History commands for ship run tracking.
/// </summary>
public static class HistoryCommand
{
    public static async Task<int> Run(string[] args, bool human)
    {
        if (args.Length == 0)
        {
            return await List(human);
        }

        return args[0] switch
        {
            "list" => await List(human),
            "add" => await Add(args.Skip(1).ToArray(), human),
            _ => ShowHelp()
        };
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill history [command]

            commands:
              list (default)        list run history
              add <json>            add a history entry (JSON)
            """);
        return 1;
    }

    private static string HistoryPath => Path.Combine(ProjectContext.MillFolder, "ship", "history.json");

    private static async Task<int> List(bool human)
    {
        var history = await LoadHistory();

        if (human)
        {
            if (history.Count == 0)
            {
                Output.Empty("No run history.");
            }
            else
            {
                foreach (var entry in history.OrderByDescending(e => e.Date).Take(20))
                {
                    Output.HistoryItem(
                        entry.Date,
                        entry.Outcome == "success",
                        entry.Issue,
                        entry.Title,
                        entry.PrUrl
                    );
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(history));
        }

        return 0;
    }

    private static async Task<int> Add(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill history add <json>");
            Console.Error.WriteLine("  or: echo '<json>' | mill history add -");
            return 1;
        }

        var json = args[0];
        if (json == "-")
        {
            json = await Console.In.ReadToEndAsync();
        }

        HistoryEntry? entry;
        try
        {
            entry = JsonHelper.Deserialize<HistoryEntry>(json);
            if (entry == null)
            {
                Console.Error.WriteLine("Invalid JSON");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Invalid JSON: {ex.Message}");
            return 1;
        }

        // Add git user if not specified
        if (string.IsNullOrEmpty(entry.GitUser))
        {
            var gitUser = await GetGitUser();
            entry = entry with { GitUser = gitUser };
        }

        var history = await LoadHistory();
        history.Add(entry);
        await SaveHistory(history);

        if (human)
        {
            Output.Success($"Added history entry for issue #{entry.Issue}");
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(new { added = true, entry }));
        }

        return 0;
    }

    private static async Task<List<HistoryEntry>> LoadHistory()
    {
        if (!File.Exists(HistoryPath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(HistoryPath);
            var data = JsonHelper.Deserialize<HistoryFile>(json);
            return data?.Runs ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task SaveHistory(List<HistoryEntry> history)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
        var data = new HistoryFile(history);
        var json = JsonHelper.Serialize(data, indented: true);
        await File.WriteAllTextAsync(HistoryPath, json);
    }

    private static async Task<string?> GetGitUser()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "config user.name",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output.Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
