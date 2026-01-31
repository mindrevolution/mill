using System.Text.Json;
using MillApi.Models;

namespace MillApi.Services;

/// <summary>
/// Service for ship workspace: runs and history.
/// </summary>
public class ShipService
{
    private readonly ILogger<ShipService> _logger;
    private readonly ProjectContext _project;
    private readonly IssueService _issueService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ShipService(
        ILogger<ShipService> logger,
        ProjectContext project,
        IssueService issueService)
    {
        _logger = logger;
        _project = project;
        _issueService = issueService;
    }

    // ─────────────────────────────────────────────────────────────────
    // Runs
    // ─────────────────────────────────────────────────────────────────

    public Task<List<Run>> GetActiveRuns()
    {
        // TODO: Track active runs in memory or via mill CLI
        return Task.FromResult(new List<Run>());
    }

    public async Task<Run?> StartRun(int issueNumber)
    {
        var issues = await _issueService.GetAll();
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
        var historyPath = Path.Combine(_project.MillFolder, "ship", "history.json");
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

    private record HistoryFile(List<HistoryEntry> Entries);
}
