using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Initialize a repository for mill.
/// Creates .mill/ folder structure.
/// </summary>
public static class InitCommand
{
    public static int Run(bool human)
    {
        var millFolder = ProjectContext.MillFolder;

        if (ProjectContext.IsInitialized)
        {
            if (human)
            {
                Output.Info($"Already initialized: {millFolder}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new { initialized = true, path = millFolder }));
            }
            return 0;
        }

        // Create folder structure
        var folders = new[]
        {
            millFolder,
            Path.Combine(millFolder, "ground", "personas"),
            Path.Combine(millFolder, "ground", "standards"),
            Path.Combine(millFolder, "ground", "concepts"),
            Path.Combine(millFolder, "ground", "design"),
            Path.Combine(millFolder, "brief", "active"),
            Path.Combine(millFolder, "shape", "drafts"),
            Path.Combine(millFolder, "ship", "work"),
        };

        foreach (var folder in folders)
        {
            Directory.CreateDirectory(folder);
        }

        // Create project.json with defaults
        var projectConfig = new ProjectConfig();
        var configPath = Path.Combine(millFolder, "project.json");
        File.WriteAllText(configPath, JsonHelper.Serialize(projectConfig, indented: true));

        // Create .gitignore for ephemeral folders
        var gitignore = """
            # mill gitignore - ephemeral local data
            brief/active/
            shape/drafts/
            ship/work/
            """;
        File.WriteAllText(Path.Combine(millFolder, ".gitignore"), gitignore);

        // Create history.json
        var historyPath = Path.Combine(millFolder, "ship", "history.json");
        File.WriteAllText(historyPath, JsonHelper.Serialize(new HistoryFile([]), indented: true));

        if (human)
        {
            Output.Success($"Initialized mill at {millFolder}");
            Output.Blank();
            Output.Title("Next steps");
            Output.Bullet("Run /mill:ground to set up product knowledge");
            Output.Bullet("Run /mill:warmup to generate context.md");
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(new
            {
                initialized = true,
                path = millFolder,
                created = folders
            }));
        }

        return 0;
    }
}
