using System.Diagnostics;
using Mill.Models;
using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Draft (spec) workspace commands.
/// </summary>
public static class DraftCommand
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
            "validate" => await Validate(args.Skip(1).ToArray(), human),
            "publish" => await Publish(args.Skip(1).ToArray(), human),
            _ => ShowHelp()
        };
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill draft <command> [options]

            commands:
              list                  list drafts
              get <slug>            get draft content
              validate <slug>       validate draft (returns relevance score)
              publish <slug>        publish draft to GitHub issue
            """);
        return 1;
    }

    private static string DraftsPath => Path.Combine(ProjectContext.MillFolder, "spec", "drafts");

    private static async Task<int> List(bool human)
    {
        if (!Directory.Exists(DraftsPath))
        {
            if (human)
            {
                Output.Empty("No drafts.");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new List<Draft>()));
            }
            return 0;
        }

        var drafts = new List<Draft>();
        foreach (var file in Directory.GetFiles(DraftsPath, "*.md"))
        {
            var info = new FileInfo(file);
            var slug = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var fm = ParseDraftFrontmatter(content);

            drafts.Add(new Draft(
                Id: slug,
                Slug: slug,
                Title: fm.title ?? MarkdownHelpers.FormatName(slug),
                Type: fm.type ?? "feature",
                Status: fm.status ?? "draft",
                UpdatedAt: info.LastWriteTime,
                Persona: fm.persona
            ));
        }

        drafts = drafts.OrderByDescending(d => d.UpdatedAt).ToList();

        if (human)
        {
            if (drafts.Count == 0)
            {
                Output.Empty("No drafts.");
            }
            else
            {
                foreach (var draft in drafts)
                {
                    Output.ListItem(draft.Type, draft.Title, draft.Slug);
                }
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(drafts));
        }

        return 0;
    }

    private static async Task<int> Get(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill draft get <slug>");
            return 1;
        }

        var slug = args[0];
        var filePath = Path.Combine(DraftsPath, $"{slug}.md");

        if (!File.Exists(filePath))
        {
            if (human)
            {
                Console.Error.WriteLine($"Draft not found: {slug}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new ErrorResponse("not_found", Slug: slug)));
            }
            return 1;
        }

        var info = new FileInfo(filePath);
        var content = await File.ReadAllTextAsync(filePath);
        var fm = ParseDraftFrontmatter(content);
        var body = MarkdownHelpers.ExtractBody(content);
        var hasRelevance = File.Exists(GetRelevancePath(slug));

        var detail = new DraftDetail(
            Id: slug,
            Slug: slug,
            Title: fm.title ?? MarkdownHelpers.FormatName(slug),
            Body: body,
            Type: fm.type ?? "feature",
            Status: fm.status ?? "draft",
            UpdatedAt: info.LastWriteTime,
            Persona: fm.persona,
            HasRelevance: hasRelevance
        );

        if (human)
        {
            Output.Title(detail.Title);
            Output.Field("Type", detail.Type);
            Output.Field("Status", detail.Status);
            Output.Field("Persona", detail.Persona);
            Output.Body(detail.Body);
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(detail));
        }

        return 0;
    }

    private static async Task<int> Validate(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill draft validate <slug>");
            return 1;
        }

        var slug = args[0];
        var filePath = Path.Combine(DraftsPath, $"{slug}.md");

        if (!File.Exists(filePath))
        {
            if (human)
            {
                Console.Error.WriteLine($"Draft not found: {slug}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new ErrorResponse("not_found", Slug: slug)));
            }
            return 1;
        }

        // Check for cached relevance
        var relevancePath = GetRelevancePath(slug);
        if (File.Exists(relevancePath))
        {
            var cached = await File.ReadAllTextAsync(relevancePath);
            if (human)
            {
                var result = JsonHelper.Deserialize<DraftValidationResult>(cached);
                if (result != null)
                {
                    Output.Score(result.Score, 10, result.Verdict);
                    Output.Field("Summary", result.Summary);
                    Output.Field("Recommendation", result.Recommendation);
                }
            }
            else
            {
                Console.WriteLine(cached);
            }
            return 0;
        }

        // No cached result - skills should run the validation prompt
        if (human)
        {
            Output.Warn("No validation result. Run /mill:spec to validate.");
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(new DraftNoValidationResponse(false, slug)));
        }

        return 0;
    }

    private static async Task<int> Publish(string[] args, bool human)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: mill draft publish <slug>");
            return 1;
        }

        var slug = args[0];
        var filePath = Path.Combine(DraftsPath, $"{slug}.md");

        if (!File.Exists(filePath))
        {
            if (human)
            {
                Console.Error.WriteLine($"Draft not found: {slug}");
            }
            else
            {
                Console.WriteLine(JsonHelper.Serialize(new DraftPublishResult(false, Error: "not_found")));
            }
            return 1;
        }

        var content = await File.ReadAllTextAsync(filePath);
        var fm = ParseDraftFrontmatter(content);
        var title = fm.title ?? MarkdownHelpers.FormatName(slug);
        var label = fm.type ?? "task";
        var body = MarkdownHelpers.ExtractBody(content);

        // Create issue using gh CLI
        var result = await CreateGitHubIssue(title, body, label);

        if (result.Success)
        {
            // Delete draft and relevance files
            File.Delete(filePath);
            var relevancePath = GetRelevancePath(slug);
            if (File.Exists(relevancePath))
            {
                File.Delete(relevancePath);
            }
        }

        if (human)
        {
            if (result.Success)
            {
                Output.Success($"Published as issue #{result.Number}");
                Output.Url(result.Url ?? "");
            }
            else
            {
                Console.Error.WriteLine($"Failed to publish: {result.Error}");
            }
        }
        else
        {
            Console.WriteLine(JsonHelper.Serialize(result));
        }

        return result.Success ? 0 : 1;
    }

    private static string GetRelevancePath(string slug) =>
        Path.Combine(DraftsPath, $"{slug}.relevance.json");

    private static async Task<DraftPublishResult> CreateGitHubIssue(string title, string body, string label)
    {
        // Write body to temp file to avoid shell escaping issues
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, body);

            var escapedTitle = title.Replace("\"", "\\\"");
            var args = $"issue create --title \"{escapedTitle}\" --body-file \"{tempFile}\" --label \"{label}\"";

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

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new DraftPublishResult(false, Error: "Failed to start gh");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return new DraftPublishResult(false, Error: error.Trim());
            }

            // gh outputs the issue URL: https://github.com/owner/repo/issues/123
            var issueUrl = output.Trim();
            int? issueNumber = null;

            if (!string.IsNullOrEmpty(issueUrl))
            {
                var lastSlash = issueUrl.LastIndexOf('/');
                if (lastSlash >= 0 && int.TryParse(issueUrl[(lastSlash + 1)..], out var num))
                {
                    issueNumber = num;
                }
            }

            return new DraftPublishResult(true, Number: issueNumber, Url: issueUrl);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    private static (string? title, string? type, string? persona, string? status) ParseDraftFrontmatter(string content)
    {
        string? title = null, type = null, persona = null, status = null;

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
                case "title": title = value; break;
                case "type": type = value; break;
                case "persona": persona = value; break;
                case "status": status = value; break;
            }
        }

        return (title, type, persona, status);
    }
}
