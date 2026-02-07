using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Brief workspace commands for idea capture.
/// </summary>
public static class BriefCommand
{
    public static async Task<int> Run(string[] args, bool human)
    {
        if (args.Length == 0)
        {
            return ShowHelp();
        }

        return args[0] switch
        {
            "list" => await List(human),
            "get" => await Get(args.Skip(1).ToArray(), human),
            "create" => await Create(args.Skip(1).ToArray(), human),
            "drop" => await Drop(args.Skip(1).ToArray(), human),
            "dropped" => await ListDropped(human),
            _ => ShowHelp()
        };
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill brief <command> [options]

            commands:
              list                  list active briefs
              get <id>              get brief details
              create <title> <intent>  create a new brief
              drop <id> <essence>   drop a brief with learned essence
              dropped               list dropped briefs
            """);
        return 1;
    }

    private static string ActivePath => Path.Combine(ProjectContext.MillFolder, "brief", "active");
    private static string DroppedPath => Path.Combine(ProjectContext.MillFolder, "brief", "dropped.json");

    private static async Task<int> List(bool human)
    {
        if (!Directory.Exists(ActivePath))
        {
            if (human)
            {
                Console.WriteLine("No active briefs.");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new List<Brief>()));
            }
            return 0;
        }

        var briefs = new List<Brief>();
        foreach (var file in Directory.GetFiles(ActivePath, "*.md"))
        {
            var info = new FileInfo(file);
            var id = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var fm = ParseBriefFrontmatter(content);

            briefs.Add(new Brief(
                Id: id,
                Title: fm.title ?? MarkdownHelpers.FormatName(id),
                Stage: fm.stage ?? "spark",
                Intent: fm.intent ?? "",
                CreatedAt: info.CreationTime,
                UpdatedAt: info.LastWriteTime,
                Persona: fm.persona,
                Concepts: fm.concepts
            ));
        }

        briefs = briefs.OrderByDescending(b => b.UpdatedAt).ToList();

        if (human)
        {
            if (briefs.Count == 0)
            {
                Console.WriteLine("No active briefs.");
            }
            else
            {
                foreach (var brief in briefs)
                {
                    Console.WriteLine($"[{brief.Stage}] {brief.Title}");
                    Console.WriteLine($"  {brief.Intent}");
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(briefs));
        }

        return 0;
    }

    private static async Task<int> Get(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill brief get <id>");
            return 1;
        }

        var id = args[0];
        var filePath = Path.Combine(ActivePath, $"{id}.md");

        if (!File.Exists(filePath))
        {
            if (human)
            {
                Console.Error.WriteLine($"Brief not found: {id}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new { error = "not_found", id }));
            }
            return 1;
        }

        var info = new FileInfo(filePath);
        var content = await File.ReadAllTextAsync(filePath);
        var fm = ParseBriefFrontmatter(content);
        var bodyContent = MarkdownHelpers.ExtractBodyContent(content);

        var detail = new BriefDetail(
            Id: id,
            Title: fm.title ?? MarkdownHelpers.FormatName(id),
            Stage: fm.stage ?? "spark",
            Intent: fm.intent ?? "",
            Content: bodyContent,
            CreatedAt: info.CreationTime,
            UpdatedAt: info.LastWriteTime,
            Persona: fm.persona,
            Concepts: fm.concepts
        );

        if (human)
        {
            Console.WriteLine($"# {detail.Title}");
            Console.WriteLine($"Stage: {detail.Stage}");
            Console.WriteLine($"Intent: {detail.Intent}");
            if (detail.Persona != null) Console.WriteLine($"Persona: {detail.Persona}");
            Console.WriteLine();
            Console.WriteLine(detail.Content);
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(detail));
        }

        return 0;
    }

    private static async Task<int> Create(string[] args, bool human)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: mill brief create <title> <intent>");
            return 1;
        }

        var title = args[0];
        var intent = args[1];

        Directory.CreateDirectory(ActivePath);

        var slug = MarkdownHelpers.Slugify(title);
        var filePath = Path.Combine(ActivePath, $"{slug}.md");

        var counter = 1;
        while (File.Exists(filePath))
            filePath = Path.Combine(ActivePath, $"{slug}-{counter++}.md");

        var actualSlug = Path.GetFileNameWithoutExtension(filePath);
        var content = $"""
            ---
            title: {title}
            stage: spark
            intent: {intent}
            ---

            ## Notes

            """;

        await File.WriteAllTextAsync(filePath, content);

        var info = new FileInfo(filePath);
        var brief = new Brief(
            Id: actualSlug,
            Title: title,
            Stage: "spark",
            Intent: intent,
            CreatedAt: info.CreationTime,
            UpdatedAt: info.LastWriteTime,
            Persona: null,
            Concepts: null
        );

        if (human)
        {
            Console.WriteLine($"Created brief: {actualSlug}");
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(brief));
        }

        return 0;
    }

    private static async Task<int> Drop(string[] args, bool human)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: mill brief drop <id> <essence>");
            return 1;
        }

        var id = args[0];
        var essence = args[1];
        var filePath = Path.Combine(ActivePath, $"{id}.md");

        if (!File.Exists(filePath))
        {
            if (human)
            {
                Console.Error.WriteLine($"Brief not found: {id}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new { error = "not_found", id }));
            }
            return 1;
        }

        var content = await File.ReadAllTextAsync(filePath);
        var fm = ParseBriefFrontmatter(content);

        var dropped = new DroppedBrief(
            Id: id,
            Essence: essence,
            DroppedAt: DateTime.UtcNow,
            OriginalTitle: fm.title ?? MarkdownHelpers.FormatName(id)
        );

        // Load existing dropped
        var droppedList = await LoadDropped();
        droppedList.Add(dropped);
        await SaveDropped(droppedList);

        // Delete the file
        File.Delete(filePath);

        if (human)
        {
            Console.WriteLine($"Dropped brief: {id}");
            Console.WriteLine($"Essence: {essence}");
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(dropped));
        }

        return 0;
    }

    private static async Task<int> ListDropped(bool human)
    {
        var dropped = await LoadDropped();

        if (human)
        {
            if (dropped.Count == 0)
            {
                Console.WriteLine("No dropped briefs.");
            }
            else
            {
                foreach (var d in dropped.OrderByDescending(x => x.DroppedAt))
                {
                    Console.WriteLine($"[{d.DroppedAt:yyyy-MM-dd}] {d.OriginalTitle}");
                    Console.WriteLine($"  {d.Essence}");
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(dropped));
        }

        return 0;
    }

    private static async Task<List<DroppedBrief>> LoadDropped()
    {
        if (!File.Exists(DroppedPath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(DroppedPath);
            var data = JsonHelper.Deserialize<DroppedBriefsFile>(json);
            return data?.Dropped ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task SaveDropped(List<DroppedBrief> briefs)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DroppedPath)!);
        var data = new DroppedBriefsFile(briefs);
        var json = JsonHelper.Serialize(data, indented: true);
        await File.WriteAllTextAsync(DroppedPath, json);
    }

    private static (string? title, string? stage, string? intent, string? persona, List<string>? concepts) ParseBriefFrontmatter(string content)
    {
        string? title = null, stage = null, intent = null, persona = null;
        List<string>? concepts = null;

        var lines = content.Split('\n');
        var inFrontmatter = false;
        var inConcepts = false;

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (inFrontmatter) break;
                inFrontmatter = true;
                continue;
            }
            if (!inFrontmatter) continue;

            if (inConcepts)
            {
                if (line.TrimStart().StartsWith("- "))
                {
                    concepts ??= [];
                    concepts.Add(line.TrimStart()[2..].Trim());
                    continue;
                }
                inConcepts = false;
            }

            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "title": title = value; break;
                case "stage": stage = value; break;
                case "intent": intent = value; break;
                case "persona": persona = string.IsNullOrEmpty(value) ? null : value; break;
                case "concepts":
                    if (string.IsNullOrEmpty(value)) inConcepts = true;
                    break;
            }
        }

        return (title, stage, intent, persona, concepts);
    }
}
