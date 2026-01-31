using System.Text.Json;
using MillApi.Models;

namespace MillApi.Services.Providers;

/// <summary>
/// GitLab issue provider using the glab CLI
/// </summary>
public class GitlabProvider : CliProviderBase, IIssueProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly MarkdownService _markdown;

    public GitlabProvider(ILogger<GitlabProvider> logger, MarkdownService markdown) : base(logger)
    {
        _markdown = markdown;
    }

    public string Name => "gitlab";

    public async Task<bool> CanHandle(string workingDir)
    {
        // Check if glab CLI is available
        if (!await IsCommandAvailable("glab"))
            return false;

        // Check if this is a GitLab repo by looking at the remote
        var result = await RunCommand("git", "remote get-url origin", workingDir);
        if (string.IsNullOrEmpty(result))
            return false;

        return result.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase) ||
               result.Contains("gitlab", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<Issue>> GetIssues(string workingDir, bool forceRefresh = false)
    {
        // TODO: Add caching similar to GithubProvider
        // glab issue list --output json
        var result = await RunCommand("glab", "issue list --output json", workingDir);
        if (string.IsNullOrEmpty(result))
            return [];

        try
        {
            var glIssues = JsonSerializer.Deserialize<List<GlIssue>>(result, JsonOptions);
            if (glIssues == null)
                return [];

            return glIssues.Select(i => new Issue(
                Number: i.Iid,
                Title: i.Title,
                Type: ExtractType(i.Labels),
                Status: i.State.Equals("opened", StringComparison.OrdinalIgnoreCase) ? "open" : "closed",
                Persona: ExtractPersona(i.Labels),
                CreatedAt: i.CreatedAt
            )).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse glab issue list output");
            return [];
        }
    }

    public async Task<IssueDetail?> GetIssueDetail(int number, string workingDir, bool forceRefresh = false)
    {
        // TODO: Add caching similar to GithubProvider
        // glab issue view {number} --output json
        var result = await RunCommand("glab", $"issue view {number} --output json", workingDir);
        if (string.IsNullOrEmpty(result))
            return null;

        try
        {
            var glIssue = JsonSerializer.Deserialize<GlIssueDetail>(result, JsonOptions);
            if (glIssue == null)
                return null;

            var body = glIssue.Description ?? "";
            return new IssueDetail(
                Number: glIssue.Iid,
                Title: glIssue.Title,
                TitleHtml: _markdown.ToHtml(glIssue.Title),
                Body: body,
                BodyHtml: _markdown.ToHtml(body),
                Type: ExtractType(glIssue.Labels),
                Status: glIssue.State.Equals("opened", StringComparison.OrdinalIgnoreCase) ? "open" : "closed",
                Persona: ExtractPersona(glIssue.Labels),
                Labels: glIssue.Labels ?? [],
                CreatedAt: glIssue.CreatedAt
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse glab issue view output for #{Number}", number);
            return null;
        }
    }

    private static string ExtractType(List<string>? labels)
    {
        if (labels == null)
            return "task";

        var typeLabels = new[] { "feature", "bug", "security", "task" };
        var label = labels.FirstOrDefault(l =>
            typeLabels.Contains(l.ToLowerInvariant()));

        return label?.ToLowerInvariant() ?? "task";
    }

    private static string? ExtractPersona(List<string>? labels)
    {
        var personaLabel = labels?.FirstOrDefault(l =>
            l.StartsWith("persona:", StringComparison.OrdinalIgnoreCase));

        return personaLabel?.Split(':').ElementAtOrDefault(1)?.Trim();
    }

    // GitLab CLI response types (glab uses different field names)
    private record GlIssue(int Iid, string Title, List<string>? Labels, string State, DateTime CreatedAt);
    private record GlIssueDetail(int Iid, string Title, string? Description, List<string>? Labels, string State, DateTime CreatedAt);
}
