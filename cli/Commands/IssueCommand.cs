using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Issue commands - wraps gh CLI for GitHub issues.
/// </summary>
public static class IssueCommand
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
            _ => ShowHelp()
        };
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill issue <command> [options]

            commands:
              list                  list open issues
              get <number>          get issue details
            """);
        return 1;
    }

    private static async Task<int> List(bool human)
    {
        var result = await RunGh("issue list --state open --json number,title,labels,createdAt --limit 50");

        if (!result.Success)
        {
            if (human)
            {
                Console.Error.WriteLine($"Failed to list issues: {result.Error}");
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
            Console.WriteLine(JsonHelper.Serialize(new List<Issue>()));
            return 0;
        }

        var issues = ghIssues.Select(i => new Issue(
            Number: i.Number,
            Title: i.Title,
            Type: ExtractType(i.Labels),
            Status: "open",
            Persona: ExtractPersona(i.Labels),
            CreatedAt: i.CreatedAt
        )).ToList();

        if (human)
        {
            if (issues.Count == 0)
            {
                Console.WriteLine("No open issues.");
            }
            else
            {
                foreach (var issue in issues)
                {
                    Console.WriteLine($"#{issue.Number} [{issue.Type}] {issue.Title}");
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(issues));
        }

        return 0;
    }

    private static async Task<int> Get(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill issue get <number>");
            return 1;
        }

        var number = args[0];
        var result = await RunGh($"issue view {number} --json number,title,body,labels,state,createdAt");

        if (!result.Success)
        {
            if (human)
            {
                Console.Error.WriteLine($"Failed to get issue: {result.Error}");
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

        var issue = new IssueDetail(
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
            Console.WriteLine($"#{issue.Number} {issue.Title}");
            Console.WriteLine($"Type: {issue.Type} | Status: {issue.Status}");
            if (issue.Labels.Count > 0)
            {
                Console.WriteLine($"Labels: {string.Join(", ", issue.Labels)}");
            }
            Console.WriteLine();
            Console.WriteLine(issue.Body);
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(issue));
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

    private static string ExtractType(List<GhLabel>? labels)
    {
        if (labels == null) return "task";
        var typeLabels = new[] { "feature", "bug", "security", "task" };
        var label = labels.FirstOrDefault(l => typeLabels.Contains(l.Name.ToLowerInvariant()));
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
