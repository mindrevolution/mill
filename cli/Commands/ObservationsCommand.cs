using Mill.Models;
using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Observations commands for the learning inbox.
/// Skills write observations during execution; /mill:ground reviews and curates them.
/// </summary>
public static class ObservationsCommand
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
            usage: mill observations <command> [options]

            commands:
              list [--source X]     list observations (optionally filter by source)
              get <id>              get observation content

            sources: ship, spec, warmup, ground
            """);
        return 1;
    }

    private static string ObservationsPath => Path.Combine(ProjectContext.MillFolder, "observations");

    private static async Task<int> List(string[] args, bool human)
    {
        // Parse --source filter
        string? sourceFilter = null;
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--source")
            {
                sourceFilter = args[i + 1];
                break;
            }
        }

        if (!Directory.Exists(ObservationsPath))
        {
            if (human)
            {
                Output.Empty("No observations.");
                Output.Blank();
                Output.Info("Observations are written during /mill:spec, /mill:ship, and /mill:warmup execution.");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new ObservationsList([], 0)));
            }
            return 0;
        }

        var observations = new List<Observation>();
        foreach (var file in Directory.GetFiles(ObservationsPath, "*.md"))
        {
            var info = new FileInfo(file);
            var id = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var fm = ParseObservationFrontmatter(content);
            var title = ExtractTitle(content);

            // Apply source filter
            if (sourceFilter != null && fm.source != sourceFilter)
            {
                continue;
            }

            observations.Add(new Observation(
                Id: id,
                Source: fm.source ?? "unknown",
                Type: fm.type ?? "discovery",
                Title: title ?? MarkdownHelpers.FormatName(id),
                Created: fm.created ?? DateOnly.FromDateTime(info.CreationTime),
                Issue: fm.issue
            ));
        }

        // Sort by created date descending (newest first)
        observations = observations.OrderByDescending(o => o.Created).ToList();

        if (human)
        {
            if (observations.Count == 0)
            {
                Output.Empty("No observations.");
            }
            else
            {
                Output.Title($"Observations ({observations.Count})");
                Output.Blank();
                foreach (var obs in observations)
                {
                    var age = GetRelativeAge(obs.Created);
                    var issueInfo = obs.Issue.HasValue ? $" (#{obs.Issue})" : "";
                    Output.Bullet($"{obs.Id,-30} {obs.Title,-35} {age}{issueInfo}");
                }
                Output.Blank();
                Output.Info("Run /mill:ground to review.");
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(new ObservationsList(observations, observations.Count)));
        }

        return 0;
    }

    private static async Task<int> Get(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill observations get <id>");
            return 1;
        }

        var id = args[0];
        var filePath = Path.Combine(ObservationsPath, $"{id}.md");

        if (!File.Exists(filePath))
        {
            if (human)
            {
                Console.Error.WriteLine($"Observation not found: {id}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new ErrorResponse("not_found", Id: id)));
            }
            return 1;
        }

        var info = new FileInfo(filePath);
        var content = await File.ReadAllTextAsync(filePath);
        var fm = ParseObservationFrontmatter(content);
        var title = ExtractTitle(content);
        var body = MarkdownHelpers.ExtractBody(content);

        var detail = new ObservationDetail(
            Id: id,
            Source: fm.source ?? "unknown",
            Type: fm.type ?? "discovery",
            Title: title ?? MarkdownHelpers.FormatName(id),
            Content: body,
            Created: fm.created ?? DateOnly.FromDateTime(info.CreationTime),
            Issue: fm.issue
        );

        if (human)
        {
            Output.Title(detail.Title);
            Output.Field("Source", detail.Source);
            Output.Field("Type", detail.Type);
            if (detail.Issue.HasValue)
            {
                Output.Field("Issue", $"#{detail.Issue}");
            }
            Output.Field("Created", detail.Created.ToString("yyyy-MM-dd"));
            Output.Body(detail.Content);
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(detail));
        }

        return 0;
    }

    private static (string? source, string? type, DateOnly? created, int? issue) ParseObservationFrontmatter(string content)
    {
        string? source = null, type = null;
        DateOnly? created = null;
        int? issue = null;

        var lines = content.Split('\n');
        var inFrontmatter = false;

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (inFrontmatter) break;
                inFrontmatter = true;
                continue;
            }
            if (!inFrontmatter) continue;

            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "source": source = value; break;
                case "type": type = value; break;
                case "created":
                    if (DateOnly.TryParse(value, out var d))
                    {
                        created = d;
                    }
                    break;
                case "issue":
                    if (int.TryParse(value, out var i))
                    {
                        issue = i;
                    }
                    break;
            }
        }

        return (source, type, created, issue);
    }

    private static string? ExtractTitle(string content)
    {
        var lines = content.Split('\n');
        var inFrontmatter = false;
        var pastFrontmatter = false;

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (inFrontmatter)
                {
                    pastFrontmatter = true;
                }
                inFrontmatter = !inFrontmatter;
                continue;
            }

            if (pastFrontmatter && line.StartsWith("# "))
            {
                return line[2..].Trim();
            }
        }

        return null;
    }

    private static string GetRelativeAge(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var days = today.DayNumber - date.DayNumber;

        return days switch
        {
            0 => "today",
            1 => "1d ago",
            < 7 => $"{days}d ago",
            < 30 => $"{days / 7}w ago",
            _ => $"{days / 30}mo ago"
        };
    }
}
