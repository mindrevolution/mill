using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Ground workspace commands for knowledge management.
/// </summary>
public static class GroundCommand
{
    private static readonly string[] Categories = ["personas", "standards", "concepts", "design"];

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
            "create" => await Create(args.Skip(1).ToArray(), human),
            _ => ShowHelp()
        };
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill ground <command> [options]

            commands:
              list [category]     list knowledge items (all or by category)
              get <category> <id> get content of a knowledge item
              create <category> <id> <content>  create a knowledge item

            categories: personas, standards, concepts, design
            """);
        return 1;
    }

    private static async Task<int> List(string[] args, bool human)
    {
        var groundPath = Path.Combine(ProjectContext.MillFolder, "ground");
        var category = args.Length > 0 ? args[0] : null;

        if (category != null && !Categories.Contains(category))
        {
            Console.Error.WriteLine($"Unknown category: {category}");
            return 1;
        }

        var categoriesToList = category != null ? [category] : Categories;
        var items = new List<KnowledgeItem>();

        foreach (var cat in categoriesToList)
        {
            var catPath = Path.Combine(groundPath, cat);
            if (!Directory.Exists(catPath)) continue;

            foreach (var file in Directory.GetFiles(catPath, "*.md"))
            {
                var info = new FileInfo(file);
                var id = Path.GetFileNameWithoutExtension(file);
                var content = await File.ReadAllTextAsync(file);
                var description = MarkdownHelpers.ExtractDescription(content);

                items.Add(new KnowledgeItem(
                    Id: id,
                    Category: cat,
                    Name: MarkdownHelpers.FormatName(id),
                    Description: description,
                    File: info.Name,
                    CreatedAt: info.CreationTime,
                    UpdatedAt: info.LastWriteTime
                ));
            }
        }

        if (human)
        {
            if (items.Count == 0)
            {
                Console.WriteLine("No knowledge items found.");
            }
            else
            {
                foreach (var item in items.OrderBy(i => i.Category).ThenBy(i => i.Name))
                {
                    Console.WriteLine($"[{item.Category}] {item.Name}");
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        Console.WriteLine($"  {item.Description[..Math.Min(80, item.Description.Length)]}...");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(items));
        }

        return 0;
    }

    private static async Task<int> Get(string[] args, bool human)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: mill ground get <category> <id>");
            return 1;
        }

        var category = args[0];
        var id = args[1];

        if (!Categories.Contains(category))
        {
            Console.Error.WriteLine($"Unknown category: {category}");
            return 1;
        }

        var filePath = Path.Combine(ProjectContext.MillFolder, "ground", category, $"{id}.md");
        if (!File.Exists(filePath))
        {
            if (human)
            {
                Console.Error.WriteLine($"Not found: {category}/{id}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new { error = "not_found", category, id }));
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
            Console.WriteLine(JsonHelper.Serialize(new { category, id, content }));
        }

        return 0;
    }

    private static async Task<int> Create(string[] args, bool human)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: mill ground create <category> <id> <content>");
            Console.Error.WriteLine("  or: echo 'content' | mill ground create <category> <id> -");
            return 1;
        }

        var category = args[0];
        var id = args[1];
        var content = args[2];

        if (!Categories.Contains(category))
        {
            Console.Error.WriteLine($"Unknown category: {category}");
            return 1;
        }

        // Read from stdin if content is "-"
        if (content == "-")
        {
            content = await Console.In.ReadToEndAsync();
        }

        var catPath = Path.Combine(ProjectContext.MillFolder, "ground", category);
        Directory.CreateDirectory(catPath);

        var filePath = Path.Combine(catPath, $"{id}.md");
        await File.WriteAllTextAsync(filePath, content);

        if (human)
        {
            Console.WriteLine($"Created: {category}/{id}");
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(new { created = true, category, id, path = filePath }));
        }

        return 0;
    }
}
