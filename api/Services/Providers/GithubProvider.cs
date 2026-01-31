using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using MillApi.Models;

namespace MillApi.Services.Providers;

/// <summary>
/// GitHub issue provider using the gh CLI with ETag-based caching.
/// ETags allow checking if data changed without consuming rate limit.
/// </summary>
public partial class GithubProvider : CliProviderBase, IIssueProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly MarkdownService _markdown;

    // Cache entries
    private readonly ConcurrentDictionary<string, CacheEntry<List<Issue>>> _issuesCache = new();
    private readonly ConcurrentDictionary<string, CacheEntry<IssueDetail>> _issueDetailCache = new();

    // Return cached data immediately if fresher than this (skip gh call entirely)
    private static readonly TimeSpan FreshCacheTtl = TimeSpan.FromMinutes(2);

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

    public async Task<List<Issue>> GetIssues(string workingDir, bool forceRefresh = false)
    {
        var cacheKey = $"issues:{workingDir}";
        _issuesCache.TryGetValue(cacheKey, out var cached);

        // Force refresh - ignore cache entirely
        if (forceRefresh)
        {
            Logger.LogDebug("Force refresh requested for issues");
            cached = null;
        }
        // Return fresh cache immediately (skip gh call)
        else if (cached != null && DateTime.UtcNow - cached.CachedAt < FreshCacheTtl)
        {
            Logger.LogDebug("Issues cache fresh, returning immediately");
            return cached.Data;
        }

        var (response, etag, notModified) = await FetchWithETag(
            "repos/{owner}/{repo}/issues?state=open&per_page=50",
            cached?.ETag,
            workingDir
        );

        if (notModified && cached != null)
        {
            Logger.LogDebug("Issues not modified (304), using cache");
            return cached.Data;
        }

        if (string.IsNullOrEmpty(response))
            return cached?.Data ?? [];

        try
        {
            var ghIssues = JsonSerializer.Deserialize<List<GhIssue>>(response, JsonOptions);
            if (ghIssues == null)
                return cached?.Data ?? [];

            var issues = ghIssues.Select(i => new Issue(
                Number: i.Number,
                Title: i.Title,
                Type: ExtractType(i.Labels),
                Status: i.State.Equals("open", StringComparison.OrdinalIgnoreCase) ? "open" : "closed",
                Persona: ExtractPersona(i.Labels),
                CreatedAt: i.CreatedAt
            )).ToList();

            // Update cache
            _issuesCache[cacheKey] = new CacheEntry<List<Issue>>(issues, etag);
            Logger.LogDebug("Issues cache updated, ETag: {ETag}", etag);

            return issues;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse issues response");
            return cached?.Data ?? [];
        }
    }

    public async Task<IssueDetail?> GetIssueDetail(int number, string workingDir, bool forceRefresh = false)
    {
        var cacheKey = $"issue:{workingDir}:{number}";
        _issueDetailCache.TryGetValue(cacheKey, out var cached);

        if (forceRefresh)
        {
            Logger.LogDebug("Force refresh requested for issue #{Number}", number);
            cached = null;
        }
        // Return fresh cache immediately (skip gh call)
        else if (cached != null && DateTime.UtcNow - cached.CachedAt < FreshCacheTtl)
        {
            Logger.LogDebug("Issue #{Number} cache fresh, returning immediately", number);
            return cached.Data;
        }

        var (response, etag, notModified) = await FetchWithETag(
            $"repos/{{owner}}/{{repo}}/issues/{number}",
            cached?.ETag,
            workingDir
        );

        if (notModified && cached != null)
        {
            Logger.LogDebug("Issue #{Number} not modified (304), using cache", number);
            return cached.Data;
        }

        if (string.IsNullOrEmpty(response))
            return cached?.Data;

        try
        {
            var ghIssue = JsonSerializer.Deserialize<GhIssueDetail>(response, JsonOptions);
            if (ghIssue == null)
                return cached?.Data;

            var body = ghIssue.Body ?? "";
            var issueDetail = new IssueDetail(
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

            // Update cache
            _issueDetailCache[cacheKey] = new CacheEntry<IssueDetail>(issueDetail, etag);
            Logger.LogDebug("Issue #{Number} cache updated, ETag: {ETag}", number, etag);

            return issueDetail;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse issue #{Number} response", number);
            return cached?.Data;
        }
    }

    public void ClearCache()
    {
        _issuesCache.Clear();
        _issueDetailCache.Clear();
        Logger.LogInformation("GitHub issue cache cleared");
    }

    /// <summary>
    /// Fetch from GitHub API with ETag support using gh api --include
    /// </summary>
    private async Task<(string? Response, string? ETag, bool NotModified)> FetchWithETag(
        string endpoint,
        string? previousETag,
        string workingDir,
        int timeoutMs = 15000)
    {
        try
        {
            // Build gh api command with --include to get headers
            var args = $"api {endpoint} --include";
            if (!string.IsNullOrEmpty(previousETag))
            {
                args += $" -H \"If-None-Match: {previousETag}\"";
            }

            Logger.LogInformation("Running: gh {Args}", args);

            var psi = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return (null, null, false);

            // Close stdin to prevent prompts
            process.StandardInput.Close();

            // Use timeout
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var exitTask = process.WaitForExitAsync();
            var timeoutTask = Task.Delay(timeoutMs);

            var completed = await Task.WhenAny(Task.WhenAll(outputTask, exitTask), timeoutTask);
            if (completed == timeoutTask)
            {
                Logger.LogWarning("gh api timed out after {Timeout}ms", timeoutMs);
                try { process.Kill(entireProcessTree: true); } catch { }
                return (null, null, false);
            }

            var output = await outputTask;
            Logger.LogInformation("gh api returned {Length} chars", output?.Length ?? 0);

            // Check for 304 Not Modified
            if (output != null && (output.Contains("HTTP/2 304") || output.Contains("304 Not Modified")))
            {
                return (null, previousETag, true);
            }

            // Parse headers and body (--include puts headers before body)
            var (headers, body) = SplitHeadersAndBody(output ?? "");

            // Extract ETag from headers
            var etag = ExtractETag(headers);

            return (body, etag, false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to fetch {Endpoint}", endpoint);
            return (null, null, false);
        }
    }

    private static (string Headers, string Body) SplitHeadersAndBody(string response)
    {
        // Headers end with double newline
        var headerEnd = response.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (headerEnd == -1)
            headerEnd = response.IndexOf("\n\n", StringComparison.Ordinal);

        if (headerEnd == -1)
            return ("", response);

        var headers = response[..headerEnd];
        var body = response[(headerEnd + (response[headerEnd] == '\r' ? 4 : 2))..];
        return (headers, body);
    }

    private static string? ExtractETag(string headers)
    {
        var match = ETagRegex().Match(headers);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"[Ee][Tt]ag:\s*""?([^""\r\n]+)""?", RegexOptions.IgnoreCase)]
    private static partial Regex ETagRegex();

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

    // Cache entry with ETag
    private record CacheEntry<T>(T Data, string? ETag, DateTime CachedAt = default)
    {
        public DateTime CachedAt { get; } = CachedAt == default ? DateTime.UtcNow : CachedAt;
    }

    // GitHub API response types
    private record GhIssue(int Number, string Title, List<GhLabel>? Labels, string State, DateTime CreatedAt);
    private record GhIssueDetail(int Number, string Title, string? Body, List<GhLabel>? Labels, string State, DateTime CreatedAt);
    private record GhLabel(string Name);
}
