using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Template commands for accessing archetypes, stacks, and spec templates.
/// </summary>
public static class TemplateCommand
{
    public static async Task<int> Run(string[] args, bool human)
    {
        if (args.Length == 0)
        {
            return ShowHelp();
        }

        return args[0] switch
        {
            "list" => await List(args.Skip(1).ToArray(), human),
            "get" => await Get(args.Skip(1).ToArray(), human),
            _ => ShowHelp()
        };
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill template <command> [options]

            commands:
              list <type>           list templates (archetypes, stacks, specs)
              get <type> <id>       get template content
            """);
        return 1;
    }

    private static async Task<int> List(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill template list <type>");
            Console.Error.WriteLine("  types: archetypes, stacks, specs");
            return 1;
        }

        var type = args[0];
        var templatesPath = GetTemplatePath(type);

        if (templatesPath == null)
        {
            Console.Error.WriteLine($"Unknown template type: {type}");
            return 1;
        }

        if (!Directory.Exists(templatesPath))
        {
            if (human)
            {
                Console.WriteLine($"No {type} templates found.");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new List<TemplateSummary>()));
            }
            return 0;
        }

        var templates = new List<TemplateSummary>();
        foreach (var file in Directory.GetFiles(templatesPath, "*.md"))
        {
            var content = await File.ReadAllTextAsync(file);
            var (id, label, summary, tags) = ParseTemplateFrontmatter(content);

            var filename = Path.GetFileNameWithoutExtension(file);
            templates.Add(new TemplateSummary(
                Id: id ?? filename,
                Label: label ?? MarkdownHelpers.FormatName(filename),
                Summary: summary ?? "",
                Tags: tags ?? []
            ));
        }

        templates = templates.OrderBy(t => t.Label).ToList();

        if (human)
        {
            foreach (var t in templates)
            {
                Console.WriteLine($"{t.Id}: {t.Label}");
                if (!string.IsNullOrEmpty(t.Summary))
                {
                    Console.WriteLine($"  {t.Summary}");
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(templates));
        }

        return 0;
    }

    private static async Task<int> Get(string[] args, bool human)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: mill template get <type> <id>");
            return 1;
        }

        var type = args[0];
        var id = args[1];

        var templatesPath = GetTemplatePath(type);
        if (templatesPath == null)
        {
            Console.Error.WriteLine($"Unknown template type: {type}");
            return 1;
        }

        var filePath = Path.Combine(templatesPath, $"{id}.md");
        if (!File.Exists(filePath))
        {
            if (human)
            {
                Console.Error.WriteLine($"Template not found: {type}/{id}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new { error = "not_found", type, id }));
            }
            return 1;
        }

        var content = await File.ReadAllTextAsync(filePath);

        if (human)
        {
            Console.WriteLine(content);
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(new TemplateContent(id, type, content)));
        }

        return 0;
    }

    private static string? GetTemplatePath(string type)
    {
        return type switch
        {
            "archetypes" => MillPaths.Archetypes,
            "stacks" => MillPaths.Stacks,
            "specs" => MillPaths.Specs,
            _ => null
        };
    }

    private static (string? id, string? label, string? summary, List<string>? tags) ParseTemplateFrontmatter(string content)
    {
        string? id = null, label = null, summary = null;
        List<string>? tags = null;

        var lines = content.Split('\n');
        var inFrontmatter = false;
        var inTags = false;

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (inFrontmatter) break;
                inFrontmatter = true;
                continue;
            }
            if (!inFrontmatter) continue;

            if (inTags)
            {
                if (line.TrimStart().StartsWith("- "))
                {
                    tags ??= [];
                    tags.Add(line.TrimStart()[2..].Trim());
                    continue;
                }
                inTags = false;
            }

            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "id": id = value; break;
                case "label": label = value; break;
                case "summary": summary = value; break;
                case "tags":
                    if (value.StartsWith('[') && value.EndsWith(']'))
                    {
                        var tagContent = value[1..^1];
                        tags = tagContent.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    }
                    else if (string.IsNullOrEmpty(value))
                    {
                        inTags = true;
                    }
                    break;
            }
        }

        return (id, label, summary, tags);
    }
}
