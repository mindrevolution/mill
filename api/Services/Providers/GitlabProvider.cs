using System.Text.Json;
using System.Text.Json.Serialization;
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

    public async Task<IssueCloseResult> CloseIssue(int number, string workingDir)
    {
        var result = await RunCommandWithResult("glab", $"issue close {number}", workingDir);
        if (!result.Success)
        {
            var error = string.IsNullOrWhiteSpace(result.Error) ? "glab issue close failed" : result.Error.Trim();
            Logger.LogWarning(
                "Failed to close GitLab issue #{Number}. Exit {ExitCode}. Error: {Error}",
                number,
                result.ExitCode,
                error
            );
            return new IssueCloseResult(false, error);
        }

        return new IssueCloseResult(true);
    }

    public async Task<IssueCreateResult> CreateIssue(string title, string body, string label, string workingDir)
    {
        // Write body to temp file to avoid shell escaping issues
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, body);

            var escapedTitle = title.Replace("\"", "\\\"");
            // glab uses --label (singular) and can specify multiple times
            var args = $"issue create --title \"{escapedTitle}\" --description \"$(cat \"{tempFile}\")\" --label \"{label}\" --yes";

            var result = await RunCommandWithResult("glab", args, workingDir);

            if (!result.Success)
            {
                var error = string.IsNullOrWhiteSpace(result.Error) ? "glab issue create failed" : result.Error.Trim();
                Logger.LogWarning(
                    "Failed to create GitLab issue. Exit {ExitCode}. Error: {Error}",
                    result.ExitCode,
                    error
                );
                return new IssueCreateResult(false, Error: error);
            }

            // glab outputs the issue URL: https://gitlab.com/owner/repo/-/issues/123
            var issueUrl = result.Output?.Trim();
            int? issueNumber = null;

            if (!string.IsNullOrEmpty(issueUrl))
            {
                var lastSlash = issueUrl.LastIndexOf('/');
                if (lastSlash >= 0 && int.TryParse(issueUrl[(lastSlash + 1)..], out var num))
                {
                    issueNumber = num;
                }
            }

            Logger.LogInformation("Created GitLab issue #{Number}: {Url}", issueNumber, issueUrl);
            return new IssueCreateResult(true, Number: issueNumber, Url: issueUrl);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    public async Task<PrCreateResult> CreatePullRequest(int issueNumber, string branch, string title, string body, string workingDir)
    {
        // Build MR body with reference to the issue
        var mrBody = $"{body}\n\nCloses #{issueNumber}";

        // Push the branch first (with upstream tracking)
        var pushResult = await RunCommandWithResult("git", $"push -u origin {branch}", workingDir);
        if (!pushResult.Success)
        {
            Logger.LogWarning(
                "Failed to push branch {Branch}. Exit {ExitCode}. Error: {Error}",
                branch,
                pushResult.ExitCode,
                pushResult.Error
            );
            return new PrCreateResult(false, Error: $"Failed to push branch: {pushResult.Error?.Trim()}");
        }

        // Create MR using glab CLI
        var escapedTitle = title.Replace("\"", "\\\"");
        var escapedBody = mrBody.Replace("\"", "\\\"").Replace("\n", "\\n");

        var args = $"mr create --source-branch \"{branch}\" --title \"{escapedTitle}\" --description \"{escapedBody}\" --yes";
        var result = await RunCommandWithResult("glab", args, workingDir);

        if (!result.Success)
        {
            var error = string.IsNullOrWhiteSpace(result.Error) ? "glab mr create failed" : result.Error.Trim();
            Logger.LogWarning(
                "Failed to create MR for issue #{Number}. Exit {ExitCode}. Error: {Error}",
                issueNumber,
                result.ExitCode,
                error
            );
            return new PrCreateResult(false, Error: error);
        }

        // glab mr create outputs the MR URL on success
        var mrUrl = result.Output?.Trim();
        Logger.LogInformation("Created MR for issue #{Number}: {Url}", issueNumber, mrUrl);

        return new PrCreateResult(true, Url: mrUrl);
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
    private record GlIssue(
        int Iid,
        string Title,
        List<string>? Labels,
        string State,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt
    );
    private record GlIssueDetail(
        int Iid,
        string Title,
        string? Description,
        List<string>? Labels,
        string State,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt
    );
}
