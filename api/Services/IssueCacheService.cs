using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using MillApi.Models;

namespace MillApi.Services;

/// <summary>
/// Caches GitHub issues using ETag-based conditional requests.
/// ETags allow us to check if data changed without consuming rate limit.
/// </summary>
public partial class IssueCacheService
{
    private readonly ILogger<IssueCacheService> _logger;
    private readonly MarkdownService _markdown;

    // Cache entries
    private readonly ConcurrentDictionary<string, CacheEntry<List<Issue>>> _issuesCache = new();
    private readonly ConcurrentDictionary<string, CacheEntry<IssueDetail>> _issueDetailCache = new();

    // Return cached data immediately if fresher than this (skip gh call entirely)
    private static readonly TimeSpan FreshCacheTtl = TimeSpan.FromMinutes(2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IssueCacheService(ILogger<IssueCacheService> logger, MarkdownService markdown)
    {
        _logger = logger;
        _markdown = markdown;
    }

    /// <summary>
    /// Get issues list with ETag caching
    /// </summary>
    public async Task<List<Issue>> GetIssues(string workingDir, bool forceRefresh = false)
    {
        var cacheKey = $"issues:{workingDir}";
        _issuesCache.TryGetValue(cacheKey, out var cached);

        // Force refresh - ignore cache entirely
        if (forceRefresh)
        {
            _logger.LogDebug("Force refresh requested for issues");
            cached = null;
        }
        // Return fresh cache immediately (skip gh call)
        else if (cached != null && DateTime.UtcNow - cached.CachedAt < FreshCacheTtl)
        {
            _logger.LogDebug("Issues cache fresh, returning immediately");
            return cached.Data;
        }

        var (response, etag, notModified) = await FetchWithETag(
            "repos/{owner}/{repo}/issues?state=open&per_page=50",
            cached?.ETag,
            workingDir
        );

        if (notModified && cached != null)
        {
            _logger.LogDebug("Issues not modified (304), using cache");
            return cached.Data;
        }

        if (string.IsNullOrEmpty(response))
            return cached?.Data ?? [];

        try
        {
            var ghIssues = JsonSerializer.Deserialize<List<GhApiIssue>>(response, JsonOptions);
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
            _logger.LogDebug("Issues cache updated, ETag: {ETag}", etag);

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse issues response");
            return cached?.Data ?? [];
        }
    }

    /// <summary>
    /// Get issue detail with ETag caching
    /// </summary>
    public async Task<IssueDetail?> GetIssueDetail(int number, string workingDir, bool forceRefresh = false)
    {
        var cacheKey = $"issue:{workingDir}:{number}";
        _issueDetailCache.TryGetValue(cacheKey, out var cached);

        if (forceRefresh)
        {
            _logger.LogDebug("Force refresh requested for issue #{Number}", number);
            cached = null;
        }
        // Return fresh cache immediately (skip gh call)
        else if (cached != null && DateTime.UtcNow - cached.CachedAt < FreshCacheTtl)
        {
            _logger.LogDebug("Issue #{Number} cache fresh, returning immediately", number);
            return cached.Data;
        }

        var (response, etag, notModified) = await FetchWithETag(
            $"repos/{{owner}}/{{repo}}/issues/{number}",
            cached?.ETag,
            workingDir
        );

        if (notModified && cached != null)
        {
            _logger.LogDebug("Issue #{Number} not modified (304), using cache", number);
            return cached.Data;
        }

        if (string.IsNullOrEmpty(response))
            return cached?.Data;

        try
        {
            var ghIssue = JsonSerializer.Deserialize<GhApiIssueDetail>(response, JsonOptions);
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
            _logger.LogDebug("Issue #{Number} cache updated, ETag: {ETag}", number, etag);

            return issueDetail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse issue #{Number} response", number);
            return cached?.Data;
        }
    }

    /// <summary>
    /// Clear all caches (for manual refresh)
    /// </summary>
    public void ClearCache()
    {
        _issuesCache.Clear();
        _issueDetailCache.Clear();
        _logger.LogInformation("Issue cache cleared");
    }

    /// <summary>
    /// Fetch from GitHub API with ETag support
    /// </summary>
    private async Task<(string? Response, string? ETag, bool NotModified)> FetchWithETag(
        string endpoint,
        string? previousETag,
        string workingDir)
    {
        try
        {
            // Build gh api command with --include to get headers
            var args = $"api {endpoint} --include";
            if (!string.IsNullOrEmpty(previousETag))
            {
                args += $" -H \"If-None-Match: {previousETag}\"";
            }

            var psi = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return (null, null, false);

            var output = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Check for 304 Not Modified
            if (output.Contains("HTTP/2 304") || output.Contains("304 Not Modified"))
            {
                return (null, previousETag, true);
            }

            // Parse headers and body (--include puts headers before body)
            var (headers, body) = SplitHeadersAndBody(output);

            // Extract ETag from headers
            var etag = ExtractETag(headers);

            return (body, etag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Endpoint}", endpoint);
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

    private static string ExtractType(List<GhApiLabel>? labels)
    {
        if (labels == null) return "task";
        var typeLabels = new[] { "feature", "bug", "security", "task" };
        var label = labels.FirstOrDefault(l => typeLabels.Contains(l.Name.ToLowerInvariant()));
        return label?.Name.ToLowerInvariant() ?? "task";
    }

    private static string? ExtractPersona(List<GhApiLabel>? labels)
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

    // GitHub API response types (slightly different from CLI)
    private record GhApiIssue(int Number, string Title, List<GhApiLabel>? Labels, string State, DateTime CreatedAt);
    private record GhApiIssueDetail(int Number, string Title, string? Body, List<GhApiLabel>? Labels, string State, DateTime CreatedAt);
    private record GhApiLabel(string Name);
}
