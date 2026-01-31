using System.Diagnostics;
using System.Text.Json;
using MillApi.Models;
using MillApi.Services.Providers;

namespace MillApi.Services;

/// <summary>
/// Service that wraps the mill CLI for API consumption.
/// Shells out to `mill` commands and parses results.
/// </summary>
public class MillService
{
    private readonly ILogger<MillService> _logger;
    private readonly IssueProviderFactory _providerFactory;
    private readonly MarkdownService _markdown;
    private readonly string _millPath;
    private string? _projectPath;

    public MillService(
        ILogger<MillService> logger,
        IConfiguration config,
        IssueProviderFactory providerFactory,
        MarkdownService markdown)
    {
        _logger = logger;
        _providerFactory = providerFactory;
        _markdown = markdown;
        _millPath = config["Mill:CliPath"] ?? "mill";
        _projectPath = config["Mill:ProjectPath"];
    }

    public void SetProjectPath(string path) => _projectPath = path;
    public string? GetProjectPath() => _projectPath;

    // ─────────────────────────────────────────────────────────────────
    // Knowledge
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<KnowledgeItem>> GetKnowledgeItems(string category)
    {
        var libraryPath = Path.Combine(_projectPath ?? ".", ".mill", "library", category);
        if (!Directory.Exists(libraryPath))
            return [];

        var items = new List<KnowledgeItem>();
        foreach (var file in Directory.GetFiles(libraryPath, "*.md"))
        {
            var info = new FileInfo(file);
            var name = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var description = ExtractDescription(content);

            items.Add(new KnowledgeItem(
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

    public async Task<string?> GetKnowledgeItemContent(string category, string id)
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
        var draftsPath = Path.Combine(_projectPath ?? ".", ".mill", "shape", "drafts");
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

    public async Task<DraftDetail?> GetDraftDetail(string id)
    {
        var draftsPath = Path.Combine(_projectPath ?? ".", ".mill", "shape", "drafts");
        var filePath = Path.Combine(draftsPath, $"{id}.md");

        if (!File.Exists(filePath))
            return null;

        var info = new FileInfo(filePath);
        var content = await File.ReadAllTextAsync(filePath);
        var (title, type, persona, status) = ParseDraftFrontmatter(content);

        // Extract body (content after frontmatter)
        var body = ExtractBody(content);

        return new DraftDetail(
            Id: id,
            Slug: id,
            Title: title ?? FormatName(id),
            Body: body,
            BodyHtml: _markdown.ToHtml(body),
            Type: type ?? "feature",
            Status: status ?? "draft",
            UpdatedAt: info.LastWriteTime,
            Persona: persona
        );
    }

    private static string ExtractBody(string content)
    {
        // Remove frontmatter (between --- markers) and return the rest
        if (!content.StartsWith("---"))
            return content.Trim();

        var endMarker = content.IndexOf("---", 3);
        if (endMarker < 0)
            return content.Trim();

        return content[(endMarker + 3)..].Trim();
    }

    // ─────────────────────────────────────────────────────────────────
    // Issues (via provider abstraction, each provider handles its own caching)
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<Issue>> GetIssues(bool forceRefresh = false)
    {
        var workingDir = _projectPath ?? Directory.GetCurrentDirectory();
        var provider = await _providerFactory.GetProvider(workingDir);

        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", workingDir);
            return [];
        }

        return await provider.GetIssues(workingDir, forceRefresh);
    }

    public async Task<IssueDetail?> GetIssueDetail(int number, bool forceRefresh = false)
    {
        var workingDir = _projectPath ?? Directory.GetCurrentDirectory();
        var provider = await _providerFactory.GetProvider(workingDir);

        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", workingDir);
            return null;
        }

        return await provider.GetIssueDetail(number, workingDir, forceRefresh);
    }

    /// <summary>
    /// Clear the issue cache for the current provider
    /// </summary>
    public async Task ClearIssueCache()
    {
        var workingDir = _projectPath ?? Directory.GetCurrentDirectory();
        var provider = await _providerFactory.GetProvider(workingDir);
        provider?.ClearCache();
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
    // Briefs
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<Brief>> GetBriefs()
    {
        var briefsPath = Path.Combine(_projectPath ?? ".", ".mill", "brief", "active");
        if (!Directory.Exists(briefsPath))
            return [];

        var briefs = new List<Brief>();
        foreach (var file in Directory.GetFiles(briefsPath, "*.md"))
        {
            var info = new FileInfo(file);
            var id = Path.GetFileNameWithoutExtension(file);
            var content = await File.ReadAllTextAsync(file);
            var frontmatter = ParseBriefFrontmatter(content);

            briefs.Add(new Brief(
                Id: id,
                Title: frontmatter.title ?? FormatName(id),
                Stage: frontmatter.stage ?? "spark",
                Intent: frontmatter.intent ?? "",
                CreatedAt: info.CreationTime,
                UpdatedAt: info.LastWriteTime,
                Persona: frontmatter.persona,
                Concepts: frontmatter.concepts
            ));
        }

        return briefs.OrderByDescending(b => b.UpdatedAt).ToList();
    }

    public async Task<BriefDetail?> GetBrief(string id)
    {
        var filePath = Path.Combine(_projectPath ?? ".", ".mill", "brief", "active", $"{id}.md");
        if (!File.Exists(filePath))
            return null;

        var info = new FileInfo(filePath);
        var content = await File.ReadAllTextAsync(filePath);
        var frontmatter = ParseBriefFrontmatter(content);
        var bodyContent = ExtractBodyContent(content);

        return new BriefDetail(
            Id: id,
            Title: frontmatter.title ?? FormatName(id),
            Stage: frontmatter.stage ?? "spark",
            Intent: frontmatter.intent ?? "",
            Content: bodyContent,
            CreatedAt: info.CreationTime,
            UpdatedAt: info.LastWriteTime,
            Persona: frontmatter.persona,
            Concepts: frontmatter.concepts
        );
    }

    public async Task<Brief> CreateBrief(string title, string intent, string? type)
    {
        var briefsPath = Path.Combine(_projectPath ?? ".", ".mill", "brief", "active");
        Directory.CreateDirectory(briefsPath);

        var slug = Slugify(title);
        var filePath = Path.Combine(briefsPath, $"{slug}.md");

        // Handle duplicates
        var counter = 1;
        while (File.Exists(filePath))
        {
            filePath = Path.Combine(briefsPath, $"{slug}-{counter++}.md");
        }

        var actualSlug = Path.GetFileNameWithoutExtension(filePath);
        var content = $"""
            ---
            title: {title}
            stage: spark
            intent: {intent}
            ---

            ## Notes

            """;

        await File.WriteAllTextAsync(filePath, content);

        var info = new FileInfo(filePath);
        return new Brief(
            Id: actualSlug,
            Title: title,
            Stage: "spark",
            Intent: intent,
            CreatedAt: info.CreationTime,
            UpdatedAt: info.LastWriteTime,
            Persona: null,
            Concepts: null
        );
    }

    public async Task<Brief?> UpdateBrief(string id, UpdateBriefRequest request)
    {
        var filePath = Path.Combine(_projectPath ?? ".", ".mill", "brief", "active", $"{id}.md");
        if (!File.Exists(filePath))
            return null;

        var content = await File.ReadAllTextAsync(filePath);
        var frontmatter = ParseBriefFrontmatter(content);
        var bodyContent = request.Content ?? ExtractBodyContent(content);

        // Update frontmatter values
        var newTitle = request.Title ?? frontmatter.title ?? FormatName(id);
        var newStage = request.Stage ?? frontmatter.stage ?? "spark";
        var newIntent = request.Intent ?? frontmatter.intent ?? "";
        var newPersona = request.Persona ?? frontmatter.persona;
        var newConcepts = request.Concepts ?? frontmatter.concepts;

        var newContent = BuildBriefContent(newTitle, newStage, newIntent, newPersona, newConcepts, bodyContent);
        await File.WriteAllTextAsync(filePath, newContent);

        var info = new FileInfo(filePath);
        return new Brief(
            Id: id,
            Title: newTitle,
            Stage: newStage,
            Intent: newIntent,
            CreatedAt: info.CreationTime,
            UpdatedAt: info.LastWriteTime,
            Persona: newPersona,
            Concepts: newConcepts
        );
    }

    public async Task<DroppedBrief?> DropBrief(string id, string essence)
    {
        var filePath = Path.Combine(_projectPath ?? ".", ".mill", "brief", "active", $"{id}.md");
        if (!File.Exists(filePath))
            return null;

        var content = await File.ReadAllTextAsync(filePath);
        var frontmatter = ParseBriefFrontmatter(content);

        var dropped = new DroppedBrief(
            Id: id,
            Essence: essence,
            DroppedAt: DateTime.UtcNow,
            OriginalTitle: frontmatter.title ?? FormatName(id)
        );

        // Add to dropped.json
        var droppedPath = Path.Combine(_projectPath ?? ".", ".mill", "brief", "dropped.json");
        var droppedList = await LoadDroppedBriefs(droppedPath);
        droppedList.Add(dropped);
        await SaveDroppedBriefs(droppedPath, droppedList);

        // Delete the active brief
        File.Delete(filePath);

        return dropped;
    }

    public async Task<Draft?> PromoteBrief(string id, string type)
    {
        var filePath = Path.Combine(_projectPath ?? ".", ".mill", "brief", "active", $"{id}.md");
        if (!File.Exists(filePath))
            return null;

        var content = await File.ReadAllTextAsync(filePath);
        var frontmatter = ParseBriefFrontmatter(content);
        var bodyContent = ExtractBodyContent(content);

        // Create draft in Shape
        var draftsPath = Path.Combine(_projectPath ?? ".", ".mill", "shape", "drafts");
        Directory.CreateDirectory(draftsPath);

        var draftPath = Path.Combine(draftsPath, $"{id}.md");
        var draftContent = $"""
            ---
            title: {frontmatter.title ?? FormatName(id)}
            type: {type}
            status: draft
            persona: {frontmatter.persona ?? ""}
            ---

            ## Intent

            {frontmatter.intent}

            ## Notes

            {bodyContent}
            """;

        await File.WriteAllTextAsync(draftPath, draftContent);

        // Delete the brief
        File.Delete(filePath);

        var info = new FileInfo(draftPath);
        return new Draft(
            Id: id,
            Slug: id,
            Title: frontmatter.title ?? FormatName(id),
            Type: type,
            Status: "draft",
            UpdatedAt: info.LastWriteTime,
            Persona: frontmatter.persona
        );
    }

    public async Task<List<DroppedBrief>> GetDroppedBriefs()
    {
        var droppedPath = Path.Combine(_projectPath ?? ".", ".mill", "brief", "dropped.json");
        return await LoadDroppedBriefs(droppedPath);
    }

    private async Task<List<DroppedBrief>> LoadDroppedBriefs(string path)
    {
        if (!File.Exists(path))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var data = JsonSerializer.Deserialize<DroppedBriefsFile>(json, JsonOptions);
            return data?.Dropped ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse dropped.json");
            return [];
        }
    }

    private async Task SaveDroppedBriefs(string path, List<DroppedBrief> briefs)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var data = new DroppedBriefsFile(briefs);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private static (string? title, string? stage, string? intent, string? persona, List<string>? concepts) ParseBriefFrontmatter(string content)
    {
        string? title = null, stage = null, intent = null, persona = null;
        List<string>? concepts = null;

        var lines = content.Split('\n');
        var inFrontmatter = false;
        var inConcepts = false;

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

            // Handle concepts array
            if (inConcepts)
            {
                if (line.TrimStart().StartsWith("- "))
                {
                    concepts ??= [];
                    concepts.Add(line.TrimStart()[2..].Trim());
                    continue;
                }
                inConcepts = false;
            }

            var parts = line.Split(':', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "title": title = value; break;
                case "stage": stage = value; break;
                case "intent": intent = value; break;
                case "persona": persona = string.IsNullOrEmpty(value) ? null : value; break;
                case "concepts":
                    if (string.IsNullOrEmpty(value))
                        inConcepts = true;
                    break;
            }
        }

        return (title, stage, intent, persona, concepts);
    }

    private static string ExtractBodyContent(string content)
    {
        var lines = content.Split('\n');
        var inFrontmatter = false;
        var pastFrontmatter = false;
        var bodyLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.Trim() == "---")
            {
                if (inFrontmatter)
                {
                    pastFrontmatter = true;
                    inFrontmatter = false;
                    continue;
                }
                inFrontmatter = true;
                continue;
            }
            if (pastFrontmatter)
                bodyLines.Add(line);
        }

        return string.Join('\n', bodyLines).Trim();
    }

    private static string BuildBriefContent(string title, string stage, string intent, string? persona, List<string>? concepts, string bodyContent)
    {
        var lines = new List<string>
        {
            "---",
            $"title: {title}",
            $"stage: {stage}",
            $"intent: {intent}"
        };

        if (!string.IsNullOrEmpty(persona))
            lines.Add($"persona: {persona}");

        if (concepts is { Count: > 0 })
        {
            lines.Add("concepts:");
            foreach (var concept in concepts)
                lines.Add($"  - {concept}");
        }

        lines.Add("---");
        lines.Add("");
        lines.Add(bodyContent);

        return string.Join('\n', lines);
    }

    private static string Slugify(string text)
    {
        return text.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("--", "-")
            .Trim('-');
    }

    private record DroppedBriefsFile(List<DroppedBrief> Dropped);

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
