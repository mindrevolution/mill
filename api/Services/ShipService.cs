using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MillApi.Models;
using MillApi.Services.Providers;

namespace MillApi.Services;

/// <summary>
/// Service for ship workspace: runs and history.
/// </summary>
public partial class ShipService
{
    private readonly ILogger<ShipService> _logger;
    private readonly ProjectContext _project;
    private readonly IssueService _issueService;
    private readonly MillPaths _paths;

    private const int MaxIterations = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ShipService(
        ILogger<ShipService> logger,
        ProjectContext project,
        IssueService issueService,
        MillPaths paths)
    {
        _logger = logger;
        _project = project;
        _issueService = issueService;
        _paths = paths;
    }

    // ─────────────────────────────────────────────────────────────────
    // Ship Run Execution
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Execute a ship run for a GitHub issue.
    /// Runs work/verify loop until done, rejected beyond limits, or aborted.
    /// </summary>
    public async Task<ShipRunResult> ExecuteShipRun(
        int issueNumber,
        ILlmProvider llmProvider,
        Action<string, int?> onProgress,
        CancellationToken ct)
    {
        var history = new List<ShipRunIteration>();
        var iteration = 0;
        string? rejectionContext = null;
        var startTime = DateTime.UtcNow;

        // Load spec from GitHub
        onProgress("Loading spec...", null);
        var issue = await _issueService.Get(issueNumber, forceRefresh: true);
        if (issue == null)
        {
            return new ShipRunResult(
                Success: false,
                Iterations: 0,
                PrUrl: null,
                AbortReason: $"Issue #{issueNumber} not found",
                History: history
            );
        }

        var specRef = $"#{issueNumber}";
        var specContent = issue.Body;

        // Read test command from Loop Contract (if present in spec)
        var testCommand = ExtractTestCommand(specContent) ?? "echo 'No test command specified'";

        while (iteration < MaxIterations)
        {
            iteration++;
            ct.ThrowIfCancellationRequested();

            // ───────────────────────────────────────────────────────────
            // Work iteration
            // ───────────────────────────────────────────────────────────
            onProgress($"Iteration {iteration}/{MaxIterations} - Working", (iteration - 1) * 100 / MaxIterations);

            var workPrompt = await BuildWorkPrompt(
                iteration,
                MaxIterations,
                issueNumber,
                specRef,
                specContent,
                rejectionContext
            );

            var workResponse = await llmProvider.Execute(workPrompt, new LlmOptions(
                WorkingDir: _project.ProjectPath,
                TimeoutMs: 300000 // 5 minutes per iteration
            ));

            if (!workResponse.Success)
            {
                _logger.LogError("Work iteration {Iteration} failed: {Error}", iteration, workResponse.Error);
                history.Add(new ShipRunIteration(iteration, "ERROR", workResponse.Error, DateTime.UtcNow));
                return new ShipRunResult(
                    Success: false,
                    Iterations: iteration,
                    PrUrl: null,
                    AbortReason: $"Work failed: {workResponse.Error}",
                    History: history
                );
            }

            var workSignal = ParseSignal(workResponse.Output);

            // Handle MILL_ABORT
            if (workSignal.Signal == "MILL_ABORT")
            {
                _logger.LogInformation("Work aborted: {Reason}", workSignal.AbortReason);
                history.Add(new ShipRunIteration(iteration, "MILL_ABORT", workSignal.AbortReason, DateTime.UtcNow));
                await WriteHistoryEntry(new HistoryEntry(
                    Date: DateTime.UtcNow,
                    Issue: issueNumber,
                    Pr: null,
                    Title: issue.Title,
                    Type: issue.Type,
                    Persona: issue.Persona,
                    Intent: ExtractIntent(issue.Body),
                    Outcome: "abandoned",
                    ContextAdded: null,
                    Iterations: iteration,
                    DurationMs: (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    PrUrl: null
                ));
                return new ShipRunResult(
                    Success: false,
                    Iterations: iteration,
                    PrUrl: null,
                    AbortReason: workSignal.AbortReason,
                    History: history
                );
            }

            // Handle MILL_CONTINUE
            if (workSignal.Signal == "MILL_CONTINUE")
            {
                _logger.LogInformation("Work iteration {Iteration} continues: {NextSlice}", iteration, workSignal.NextSlice);
                history.Add(new ShipRunIteration(iteration, "MILL_CONTINUE", workSignal.NextSlice, DateTime.UtcNow));
                rejectionContext = null; // Clear rejection context on successful continue
                continue;
            }

            // Handle MILL_VERIFY
            if (workSignal.Signal == "MILL_VERIFY")
            {
                _logger.LogInformation("Work iteration {Iteration} ready for verification", iteration);
                history.Add(new ShipRunIteration(iteration, "MILL_VERIFY", workSignal.Summary, DateTime.UtcNow));

                // ───────────────────────────────────────────────────────
                // Verification
                // ───────────────────────────────────────────────────────
                onProgress($"Iteration {iteration}/{MaxIterations} - Verifying", iteration * 100 / MaxIterations);

                var verifyPrompt = await BuildVerifyPrompt(
                    specRef,
                    specContent,
                    testCommand
                );

                var verifyResponse = await llmProvider.Execute(verifyPrompt, new LlmOptions(
                    WorkingDir: _project.ProjectPath,
                    TimeoutMs: 180000 // 3 minutes for verification
                ));

                if (!verifyResponse.Success)
                {
                    _logger.LogError("Verification failed: {Error}", verifyResponse.Error);
                    history.Add(new ShipRunIteration(iteration, "VERIFY_ERROR", verifyResponse.Error, DateTime.UtcNow));
                    return new ShipRunResult(
                        Success: false,
                        Iterations: iteration,
                        PrUrl: null,
                        AbortReason: $"Verification failed: {verifyResponse.Error}",
                        History: history
                    );
                }

                var verifySignal = ParseSignal(verifyResponse.Output);

                // Handle MILL_DONE
                if (verifySignal.Signal == "MILL_DONE")
                {
                    _logger.LogInformation("Verification passed - creating PR");
                    history.Add(new ShipRunIteration(iteration, "MILL_DONE", "Verification passed", DateTime.UtcNow));

                    // Create PR
                    onProgress("Creating PR...", 95);
                    var prResult = await _issueService.CreatePullRequest(
                        issueNumber,
                        workSignal.Branch ?? $"issue-{issueNumber}",
                        workSignal.Title ?? $"#{issueNumber}: {issue.Title}",
                        workSignal.Summary ?? "Implementation complete"
                    );

                    if (!prResult.Success)
                    {
                        _logger.LogError("PR creation failed: {Error}", prResult.Error);
                        await WriteHistoryEntry(new HistoryEntry(
                            Date: DateTime.UtcNow,
                            Issue: issueNumber,
                            Pr: null,
                            Title: issue.Title,
                            Type: issue.Type,
                            Persona: issue.Persona,
                            Intent: ExtractIntent(issue.Body),
                            Outcome: "abandoned",
                            ContextAdded: null,
                            Iterations: iteration,
                            DurationMs: (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                            PrUrl: null
                        ));
                        return new ShipRunResult(
                            Success: false,
                            Iterations: iteration,
                            PrUrl: null,
                            AbortReason: $"PR creation failed: {prResult.Error}",
                            History: history
                        );
                    }

                    // Extract PR number from URL (e.g., https://github.com/owner/repo/pull/123)
                    int? prNumber = null;
                    if (prResult.Url != null)
                    {
                        var parts = prResult.Url.Split('/');
                        if (parts.Length > 0 && int.TryParse(parts[^1], out var num))
                        {
                            prNumber = num;
                        }
                    }

                    await WriteHistoryEntry(new HistoryEntry(
                        Date: DateTime.UtcNow,
                        Issue: issueNumber,
                        Pr: prNumber,
                        Title: issue.Title,
                        Type: issue.Type,
                        Persona: issue.Persona,
                        Intent: ExtractIntent(issue.Body),
                        Outcome: "shipped",
                        ContextAdded: null,
                        Iterations: iteration,
                        DurationMs: (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                        PrUrl: prResult.Url
                    ));

                    return new ShipRunResult(
                        Success: true,
                        Iterations: iteration,
                        PrUrl: prResult.Url,
                        AbortReason: null,
                        History: history
                    );
                }

                // Handle MILL_REJECTED
                if (verifySignal.Signal == "MILL_REJECTED")
                {
                    _logger.LogInformation("Verification rejected: {Suggestion}", verifySignal.Suggestion);
                    history.Add(new ShipRunIteration(iteration, "MILL_REJECTED", verifySignal.Suggestion, DateTime.UtcNow));

                    // Build rejection context for next iteration
                    rejectionContext = BuildRejectionContext(verifySignal);
                    continue;
                }

                // Unknown verify signal - treat as failure
                _logger.LogWarning("Unknown verify signal: {Signal}", verifySignal.Signal);
                history.Add(new ShipRunIteration(iteration, "UNKNOWN", verifySignal.Signal, DateTime.UtcNow));
                return new ShipRunResult(
                    Success: false,
                    Iterations: iteration,
                    PrUrl: null,
                    AbortReason: $"Unexpected verification signal: {verifySignal.Signal}",
                    History: history
                );
            }

            // Unknown work signal
            _logger.LogWarning("Unknown work signal: {Signal}", workSignal.Signal);
            history.Add(new ShipRunIteration(iteration, "UNKNOWN", workSignal.Signal, DateTime.UtcNow));
            return new ShipRunResult(
                Success: false,
                Iterations: iteration,
                PrUrl: null,
                AbortReason: $"Unexpected work signal: {workSignal.Signal}",
                History: history
            );
        }

        // Max iterations reached without success
        _logger.LogWarning("Max iterations ({Max}) reached without completion", MaxIterations);
        await WriteHistoryEntry(new HistoryEntry(
            Date: DateTime.UtcNow,
            Issue: issueNumber,
            Pr: null,
            Title: issue.Title,
            Type: issue.Type,
            Persona: issue.Persona,
            Intent: ExtractIntent(issue.Body),
            Outcome: "abandoned",
            ContextAdded: null,
            Iterations: iteration,
            DurationMs: (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
            PrUrl: null
        ));
        return new ShipRunResult(
            Success: false,
            Iterations: iteration,
            PrUrl: null,
            AbortReason: $"Max iterations ({MaxIterations}) reached",
            History: history
        );
    }

    // ─────────────────────────────────────────────────────────────────
    // Prompt Building
    // ─────────────────────────────────────────────────────────────────

    private async Task<string> BuildWorkPrompt(
        int iteration,
        int maxIterations,
        int issueNumber,
        string specRef,
        string specContent,
        string? rejectionContext)
    {
        var templatePath = Path.Combine(_paths.ShipPrompts, "loop-iterate.md");
        if (!File.Exists(templatePath))
            throw new InvalidOperationException("loop-iterate.md prompt template not found");

        var template = await File.ReadAllTextAsync(templatePath);

        var prompt = template
            .Replace("{{ITERATION}}", iteration.ToString())
            .Replace("{{MAX_ITERATIONS}}", maxIterations.ToString())
            .Replace("{{ISSUE_NUMBER}}", issueNumber.ToString())
            .Replace("{{SPEC_REF}}", specRef)
            .Replace("{{SPEC_CONTENT}}", specContent);

        if (!string.IsNullOrEmpty(rejectionContext))
        {
            prompt += $"\n\n## Previous Verification Failure\n\n{rejectionContext}";
        }

        return prompt;
    }

    private async Task<string> BuildVerifyPrompt(
        string specRef,
        string specContent,
        string testCommand)
    {
        var templatePath = Path.Combine(_paths.ShipPrompts, "loop-verify.md");
        if (!File.Exists(templatePath))
            throw new InvalidOperationException("loop-verify.md prompt template not found");

        var template = await File.ReadAllTextAsync(templatePath);

        return template
            .Replace("{{SPEC_REF}}", specRef)
            .Replace("{{SPEC_CONTENT}}", specContent)
            .Replace("{{TEST_COMMAND}}", testCommand);
    }

    private static string BuildRejectionContext(ParsedSignal signal)
    {
        var sb = new StringBuilder();

        if (signal.Blockers?.Count > 0)
        {
            sb.AppendLine("**Blockers:**");
            foreach (var blocker in signal.Blockers)
            {
                sb.AppendLine($"- {blocker}");
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(signal.Suggestion))
        {
            sb.AppendLine($"**Most important fix:** {signal.Suggestion}");
        }

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────
    // Signal Parsing
    // ─────────────────────────────────────────────────────────────────

    private record ParsedSignal(
        string Signal,
        string? Branch = null,
        string? Title = null,
        string? Summary = null,
        string? NextSlice = null,
        string? AbortReason = null,
        List<string>? Blockers = null,
        string? Suggestion = null
    );

    private ParsedSignal ParseSignal(string output)
    {
        // Look for signal patterns in output
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // MILL_DONE (simple)
            if (trimmed == "MILL_DONE")
            {
                return new ParsedSignal("MILL_DONE");
            }

            // MILL_ABORT: reason
            if (trimmed.StartsWith("MILL_ABORT:"))
            {
                var reason = trimmed["MILL_ABORT:".Length..].Trim();
                return new ParsedSignal("MILL_ABORT", AbortReason: reason);
            }

            // MILL_CONTINUE with next slice
            if (trimmed == "MILL_CONTINUE")
            {
                // Look for "Next slice:" in following lines
                var idx = Array.IndexOf(lines, line);
                string? nextSlice = null;
                for (var i = idx + 1; i < lines.Length && i < idx + 5; i++)
                {
                    var nextLine = lines[i].Trim();
                    if (nextLine.StartsWith("Next slice:", StringComparison.OrdinalIgnoreCase))
                    {
                        nextSlice = nextLine["Next slice:".Length..].Trim();
                        break;
                    }
                }
                return new ParsedSignal("MILL_CONTINUE", NextSlice: nextSlice);
            }

            // MILL_VERIFY with JSON metadata
            if (trimmed == "MILL_VERIFY")
            {
                // Look for JSON block after MILL_VERIFY
                var idx = Array.IndexOf(lines, line);
                var jsonBuilder = new StringBuilder();
                var inJson = false;
                for (var i = idx + 1; i < lines.Length; i++)
                {
                    var nextLine = lines[i];
                    if (nextLine.Trim().StartsWith('{'))
                    {
                        inJson = true;
                    }
                    if (inJson)
                    {
                        jsonBuilder.AppendLine(nextLine);
                        if (nextLine.Trim().EndsWith('}'))
                        {
                            break;
                        }
                    }
                }

                if (jsonBuilder.Length > 0)
                {
                    try
                    {
                        var json = jsonBuilder.ToString();
                        var meta = JsonSerializer.Deserialize<VerifyMetadata>(json, JsonOptions);
                        return new ParsedSignal(
                            "MILL_VERIFY",
                            Branch: meta?.Branch,
                            Title: meta?.Title,
                            Summary: meta?.Summary
                        );
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Failed to parse MILL_VERIFY metadata: {Error}", ex.Message);
                    }
                }

                return new ParsedSignal("MILL_VERIFY");
            }

            // MILL_REJECTED with JSON
            if (trimmed == "MILL_REJECTED")
            {
                var idx = Array.IndexOf(lines, line);
                var jsonBuilder = new StringBuilder();
                var inJson = false;
                for (var i = idx + 1; i < lines.Length; i++)
                {
                    var nextLine = lines[i];
                    if (nextLine.Trim().StartsWith('{'))
                    {
                        inJson = true;
                    }
                    if (inJson)
                    {
                        jsonBuilder.AppendLine(nextLine);
                        if (nextLine.Trim().EndsWith('}'))
                        {
                            break;
                        }
                    }
                }

                if (jsonBuilder.Length > 0)
                {
                    try
                    {
                        var json = jsonBuilder.ToString();
                        var meta = JsonSerializer.Deserialize<RejectedMetadata>(json, JsonOptions);
                        return new ParsedSignal(
                            "MILL_REJECTED",
                            Blockers: meta?.Blockers,
                            Suggestion: meta?.Suggestion
                        );
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Failed to parse MILL_REJECTED metadata: {Error}", ex.Message);
                    }
                }

                return new ParsedSignal("MILL_REJECTED");
            }
        }

        // No recognized signal found
        return new ParsedSignal("UNKNOWN");
    }

    private record VerifyMetadata(string? Branch, string? Title, string? Summary, string? Verification);
    private record RejectedMetadata(List<string>? Blockers, List<string>? Improvements, string? Suggestion);

    /// <summary>
    /// Extract test command from spec's Loop Contract section.
    /// </summary>
    private static string? ExtractTestCommand(string specContent)
    {
        // Look for "Test Command:" or "test_command:" in the spec
        var match = TestCommandRegex().Match(specContent);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return null;
    }

    [GeneratedRegex(@"[Tt]est[_ ][Cc]ommand:\s*`?([^`\n]+)`?", RegexOptions.Multiline)]
    private static partial Regex TestCommandRegex();

    // ─────────────────────────────────────────────────────────────────
    // Legacy Run Methods (kept for compatibility)
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

    public async Task WriteHistoryEntry(HistoryEntry entry)
    {
        var historyPath = Path.Combine(_project.MillFolder, "ship", "history.json");

        // Ensure directory exists
        var dir = Path.GetDirectoryName(historyPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Read existing entries
        var entries = await GetHistory();
        var mutableEntries = entries.ToList();

        // Prepend new entry (most recent first)
        mutableEntries.Insert(0, entry);

        // Write back
        var historyFile = new HistoryFile(mutableEntries);
        var json = JsonSerializer.Serialize(historyFile, JsonOptions);
        await File.WriteAllTextAsync(historyPath, json);

        _logger.LogInformation("Wrote history entry for issue #{Issue}", entry.Issue);
    }

    private static string ExtractIntent(string body)
    {
        // Take first non-empty line as intent, truncated
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.FirstOrDefault()?.Trim() ?? "";
        // Remove markdown headers
        if (firstLine.StartsWith('#'))
        {
            firstLine = firstLine.TrimStart('#').Trim();
        }
        // Truncate if too long
        return firstLine.Length > 200 ? firstLine[..200] + "..." : firstLine;
    }

    private record HistoryFile(List<HistoryEntry> Entries);
}
