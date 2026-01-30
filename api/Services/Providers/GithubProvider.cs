using System.Text.Json;
using MillApi.Models;

namespace MillApi.Services.Providers;

/// <summary>
/// GitHub issue provider using the gh CLI
/// </summary>
public class GithubProvider : CliProviderBase, IIssueProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly MarkdownService _markdown;

    public GithubProvider(ILogger<GithubProvider> logger, MarkdownService markdown) : base(logger)
    {
        _markdown = markdown;
    }

    public string Name => "github";

    public async Task<bool> CanHandle(string workingDir)
    {
        // Check if gh CLI is available
        if (!await IsCommandAvailable("gh"))
            return false;

        // Check if this is a GitHub repo by looking at the remote
        var result = await RunCommand("git", "remote get-url origin", workingDir);
        if (string.IsNullOrEmpty(result))
            return false;

        return result.Contains("github.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<Issue>> GetIssues(string workingDir)
    {
        var result = await RunCommand("gh", "issue list --json number,title,labels,state,createdAt --limit 50", workingDir);
        if (string.IsNullOrEmpty(result))
            return [];

        try
        {
            var ghIssues = JsonSerializer.Deserialize<List<GhIssue>>(result, JsonOptions);
            if (ghIssues == null)
                return [];

            return ghIssues.Select(i => new Issue(
                Number: i.Number,
                Title: i.Title,
                Type: ExtractType(i.Labels),
                Status: i.State.Equals("open", StringComparison.OrdinalIgnoreCase) ? "open" : "closed",
                Persona: ExtractPersona(i.Labels),
                CreatedAt: i.CreatedAt
            )).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse gh issue list output");
            return [];
        }
    }

    public async Task<IssueDetail?> GetIssueDetail(int number, string workingDir)
    {
        var result = await RunCommand("gh", $"issue view {number} --json number,title,body,labels,state,createdAt", workingDir);
        if (string.IsNullOrEmpty(result))
            return null;

        try
        {
            var ghIssue = JsonSerializer.Deserialize<GhIssueDetail>(result, JsonOptions);
            if (ghIssue == null)
                return null;

            var body = ghIssue.Body ?? "";
            return new IssueDetail(
                Number: ghIssue.Number,
                Title: ghIssue.Title,
                Body: body,
                BodyHtml: _markdown.ToHtml(body),
                Type: ExtractType(ghIssue.Labels),
                Status: ghIssue.State.Equals("open", StringComparison.OrdinalIgnoreCase) ? "open" : "closed",
                Persona: ExtractPersona(ghIssue.Labels),
                Labels: ghIssue.Labels?.Select(l => l.Name).ToList() ?? [],
                CreatedAt: ghIssue.CreatedAt
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse gh issue view output for #{Number}", number);
            return null;
        }
    }

    private static string ExtractType(List<GhLabel>? labels)
    {
        if (labels == null)
            return "task";

        var typeLabels = new[] { "feature", "bug", "security", "task" };
        var label = labels.FirstOrDefault(l =>
            typeLabels.Contains(l.Name.ToLowerInvariant()));

        return label?.Name.ToLowerInvariant() ?? "task";
    }

    private static string? ExtractPersona(List<GhLabel>? labels)
    {
        var personaLabel = labels?.FirstOrDefault(l =>
            l.Name.StartsWith("persona:", StringComparison.OrdinalIgnoreCase));

        return personaLabel?.Name.Split(':').ElementAtOrDefault(1)?.Trim();
    }

    // GitHub CLI response types
    private record GhIssue(int Number, string Title, List<GhLabel>? Labels, string State, DateTime CreatedAt);
    private record GhIssueDetail(int Number, string Title, string? Body, List<GhLabel>? Labels, string State, DateTime CreatedAt);
    private record GhLabel(string Name);
}
