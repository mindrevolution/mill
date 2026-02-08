using Mill.Models;
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
            // Observations (learning inbox)
            Path.Combine(millFolder, "observations"),
            // Ground - expanded (10 categories)
            Path.Combine(millFolder, "ground", "strategic"),
            Path.Combine(millFolder, "ground", "personas"),
            Path.Combine(millFolder, "ground", "rules"),
            Path.Combine(millFolder, "ground", "decisions"),
            Path.Combine(millFolder, "ground", "vocabulary"),
            Path.Combine(millFolder, "ground", "stack"),
            Path.Combine(millFolder, "ground", "schema"),
            Path.Combine(millFolder, "ground", "design"),
            Path.Combine(millFolder, "ground", "patterns"),
            Path.Combine(millFolder, "ground", "debt"),
            // Rest unchanged
            Path.Combine(millFolder, "idea", "active"),
            Path.Combine(millFolder, "spec", "drafts"),
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
            observations/
            idea/active/
            spec/drafts/
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
