using System.Diagnostics;
using System.Text.Json;
using Mill.Api.Models;
using Mill.Api.Services.Providers;

namespace Mill.Api.Services;

/// <summary>
/// Service that wraps the mill CLI for API consumption.
/// Shells out to `mill` commands and parses results.
/// </summary>
public class MillService
{
    private readonly ILogger<MillService> _logger;
    private readonly IssueProviderFactory _providerFactory;
    private readonly string _millPath;
    private string? _projectPath;

    public MillService(
        ILogger<MillService> logger,
        IConfiguration config,
        IssueProviderFactory providerFactory)
    {
        _logger = logger;
        _providerFactory = providerFactory;
        _millPath = config["Mill:CliPath"] ?? "mill";
        _projectPath = config["Mill:ProjectPath"];
    }

    public void SetProjectPath(string path) => _projectPath = path;
    public string? GetProjectPath() => _projectPath;

    // ─────────────────────────────────────────────────────────────────
    // Library
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<LibraryItem>> GetLibraryItems(string category)
    {
        var libraryPath = Path.Combine(_projectPath ?? ".", ".mill", "library", category);
        if (!Directory.Exists(libraryPath))
            return [];

        var items = new List<LibraryItem>();
        foreach (var file in Directory.GetFiles(libraryPath, "*.md"))
        {
            var info = new FileInfo(file);
            var name = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var description = ExtractDescription(content);

            items.Add(new LibraryItem(
                Id: name,
                Category: category,
                Name: FormatName(name),
                Description: description,
                File: info.Name,
                CreatedAt: info.CreationTime,
                UpdatedAt: info.LastWriteTime
            ));
        }

        return items;
    }

    public async Task<string?> GetLibraryItemContent(string category, string id)
    {
        var filePath = Path.Combine(_projectPath ?? ".", ".mill", "library", category, $"{id}.md");
        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllTextAsync(filePath);
    }

    // ─────────────────────────────────────────────────────────────────
    // Drafts
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<Draft>> GetDrafts()
    {
        var draftsPath = Path.Combine(_projectPath ?? ".", ".mill", "drafts");
        if (!Directory.Exists(draftsPath))
            return [];

        var drafts = new List<Draft>();
        foreach (var file in Directory.GetFiles(draftsPath, "*.md"))
        {
            var info = new FileInfo(file);
            var slug = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var (title, type, persona, status) = ParseDraftFrontmatter(content);

            drafts.Add(new Draft(
                Id: slug,
                Slug: slug,
                Title: title ?? FormatName(slug),
                Type: type ?? "feature",
                Status: status ?? "draft",
                UpdatedAt: info.LastWriteTime,
                Persona: persona
            ));
        }

        return drafts.OrderByDescending(d => d.UpdatedAt).ToList();
    }

    // ─────────────────────────────────────────────────────────────────
    // Issues (via provider - GitHub, GitLab, etc.)
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<Issue>> GetIssues()
    {
        var workingDir = _projectPath ?? Directory.GetCurrentDirectory();
        var provider = await _providerFactory.GetProvider(workingDir);

        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", workingDir);
            return [];
        }

        return await provider.GetIssues(workingDir);
    }

    public async Task<IssueDetail?> GetIssueDetail(int number)
    {
        var workingDir = _projectPath ?? Directory.GetCurrentDirectory();
        var provider = await _providerFactory.GetProvider(workingDir);

        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", workingDir);
            return null;
        }

        return await provider.GetIssueDetail(number, workingDir);
    }

    /// <summary>
    /// Get the name of the detected issue provider (github, gitlab, etc.)
    /// </summary>
    public async Task<string?> GetIssueProviderName()
    {
        var workingDir = _projectPath ?? Directory.GetCurrentDirectory();
        var provider = await _providerFactory.GetProvider(workingDir);
        return provider?.Name;
    }

    // ─────────────────────────────────────────────────────────────────
    // Runs
    // ─────────────────────────────────────────────────────────────────

    public Task<List<Run>> GetActiveRuns()
    {
        // TODO: Track active runs in memory or via mill CLI
        // For now, return empty - runs are managed by CLI
        return Task.FromResult(new List<Run>());
    }

    public async Task<Run?> StartRun(int issueNumber)
    {
        // This would start `mill run {issue}` in background
        // For now, just validate the issue exists
        var issues = await GetIssues();
        var issue = issues.FirstOrDefault(i => i.Number == issueNumber);
        if (issue == null)
            return null;

        var runId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("Starting run {RunId} for issue #{Issue}", runId, issueNumber);

        // TODO: Actually start the mill run process
        return new Run(
            Id: runId,
            Issue: issueNumber,
            Title: issue.Title,
            Status: "starting",
            Iteration: 0,
            MaxIterations: 5,
            StartedAt: DateTime.UtcNow
        );
    }

    // ─────────────────────────────────────────────────────────────────
    // History
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<HistoryEntry>> GetHistory()
    {
        var historyPath = Path.Combine(_projectPath ?? ".", ".mill", "history.json");
        if (!File.Exists(historyPath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(historyPath);
            var history = JsonSerializer.Deserialize<HistoryFile>(json, JsonOptions);
            return history?.Entries ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse history.json");
            return [];
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private async Task<string?> RunCommand(string command, string args, string? workingDir = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run command: {Command} {Args}", command, args);
            return null;
        }
    }

    private static string ExtractDescription(string content)
    {
        // Skip frontmatter and get first paragraph
        var lines = content.Split('\n');
        var inFrontmatter = false;
        var description = new List<string>();

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                inFrontmatter = !inFrontmatter;
                continue;
            }
            if (inFrontmatter)
                continue;
            if (string.IsNullOrWhiteSpace(line) && description.Count > 0)
                break;
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
                description.Add(line.Trim());
        }

        return string.Join(" ", description).Trim();
    }

    private static string FormatName(string slug)
    {
        return string.Join(" ", slug.Split('-').Select(w =>
            w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));
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
                if (inFrontmatter)
                    break;
                inFrontmatter = true;
                continue;
            }
            if (!inFrontmatter)
                continue;

            var parts = line.Split(':', 2);
            if (parts.Length != 2)
                continue;

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

    private record HistoryFile(List<HistoryEntry> Entries);
}
