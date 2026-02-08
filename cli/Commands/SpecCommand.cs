using System.Diagnostics;
using System.Text.Json;
using Mill.Models;
using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Spec commands - lists published specs from GitHub issues.
/// Only returns issues with spec type labels (feature, bug, security, task).
/// </summary>
public static class SpecCommand
{
    private static readonly string[] SpecTypes = ["feature", "bug", "security", "task"];

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
            _ => ShowHelp()
        };
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill spec <command> [options]

            commands:
              list                  list open specs
              get <number>          get spec details
            """);
        return 1;
    }

    private static async Task<int> List(bool human)
    {
        var result = await RunGh("issue list --state open --json number,title,labels,createdAt --limit 100");

        if (!result.Success)
        {
            if (human)
            {
                Console.Error.WriteLine($"Failed to list specs: {result.Error}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new { error = result.Error }));
            }
            return 1;
        }

        var ghIssues = JsonSerializer.Deserialize<List<GhIssueListItem>>(result.Output, JsonOptions);
        if (ghIssues == null)
        {
            Console.WriteLine(JsonHelper.Serialize(new List<Spec>()));
            return 0;
        }

        // Filter to only include issues with spec type labels
        var specs = ghIssues
            .Where(i => HasSpecTypeLabel(i.Labels))
            .Select(i => new Spec(
                Number: i.Number,
                Title: i.Title,
                Type: ExtractType(i.Labels),
                Status: "open",
                Persona: ExtractPersona(i.Labels),
                CreatedAt: i.CreatedAt
            ))
            .ToList();

        if (human)
        {
            if (specs.Count == 0)
            {
                Output.Empty("No open specs.");
            }
            else
            {
                foreach (var spec in specs)
                {
                    Output.NumberedItem(spec.Number, spec.Type, spec.Title);
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(specs));
        }

        return 0;
    }

    private static async Task<int> Get(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill spec get <number>");
            return 1;
        }

        var number = args[0];
        var result = await RunGh($"issue view {number} --json number,title,body,labels,state,createdAt");

        if (!result.Success)
        {
            if (human)
            {
                Console.Error.WriteLine($"Failed to get spec: {result.Error}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new { error = result.Error }));
            }
            return 1;
        }

        var ghIssue = JsonSerializer.Deserialize<GhIssueViewItem>(result.Output, JsonOptions);
        if (ghIssue == null)
        {
            Console.WriteLine(JsonHelper.Serialize(new { error = "parse_error" }));
            return 1;
        }

        // Warn if not a spec type
        if (!HasSpecTypeLabel(ghIssue.Labels))
        {
            if (human)
            {
                Output.Warn($"#{number} is not a spec (missing type label)");
            }
        }

        var spec = new SpecDetail(
            Number: ghIssue.Number,
            Title: ghIssue.Title,
            Body: ghIssue.Body ?? "",
            Type: ExtractType(ghIssue.Labels),
            Status: ghIssue.State.Equals("open", StringComparison.OrdinalIgnoreCase) ? "open" : "closed",
            Persona: ExtractPersona(ghIssue.Labels),
            Labels: ghIssue.Labels?.Select(l => l.Name).ToList() ?? [],
            CreatedAt: ghIssue.CreatedAt
        );

        if (human)
        {
            Output.Title($"#{spec.Number} {spec.Title}");
            Output.Field("Type", spec.Type);
            Output.Field("Status", spec.Status);
            Output.Field("Persona", spec.Persona);
            Output.Body(spec.Body);
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(spec));
        }

        return 0;
    }

    private static async Task<(bool Success, string Output, string Error)> RunGh(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = args,
            WorkingDirectory = ProjectContext.ProjectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, "", "Failed to start gh");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode == 0, output, error.Trim());
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

    private static bool HasSpecTypeLabel(List<GhLabel>? labels)
    {
        if (labels == null) return false;
        return labels.Any(l => SpecTypes.Contains(l.Name.ToLowerInvariant()));
    }

    private static string ExtractType(List<GhLabel>? labels)
    {
        if (labels == null) return "task";
        var label = labels.FirstOrDefault(l => SpecTypes.Contains(l.Name.ToLowerInvariant()));
        return label?.Name.ToLowerInvariant() ?? "task";
    }

    private static string? ExtractPersona(List<GhLabel>? labels)
    {
        var personaLabel = labels?.FirstOrDefault(l =>
            l.Name.StartsWith("persona:", StringComparison.OrdinalIgnoreCase));
        return personaLabel?.Name.Split(':').ElementAtOrDefault(1)?.Trim();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private record GhIssueListItem(
        int Number,
        string Title,
        List<GhLabel>? Labels,
        DateTime CreatedAt
    );

    private record GhIssueViewItem(
        int Number,
        string Title,
        string? Body,
        string State,
        List<GhLabel>? Labels,
        DateTime CreatedAt
    );

    private record GhLabel(string Name);
}
