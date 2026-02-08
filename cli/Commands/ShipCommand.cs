using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Mill.Models;
using Mill.Services;

namespace Mill.Commands;

/// <summary>
/// Ship command — CLI-orchestrated work loop.
/// Creates a worktree, iterates with Claude, verifies independently, creates PR.
/// </summary>
public static class ShipCommand
{
    private const int MaxIterations = 20;
    private const int ClaudeTimeoutMs = 600_000; // 10 minutes per invocation

    private static string WorkPath => Path.Combine(ProjectContext.MillFolder, "ship", "work");
    private static string HistoryPath => Path.Combine(ProjectContext.MillFolder, "ship", "history.json");
    private static string ContextPath => Path.Combine(ProjectContext.MillFolder, "context.md");

    public static async Task<int> Run(string[] args, bool human)
    {
        if (args.Length == 0 || args[0] is "--help" or "help")
        {
            return ShowHelp();
        }

        if (!int.TryParse(args[0], out var issueNumber))
        {
            Console.Error.WriteLine($"Invalid issue number: {args[0]}");
            return 1;
        }

        if (!ProjectContext.IsInitialized)
        {
            if (human) Console.Error.WriteLine("Not a mill project. Run: mill init");
            else Console.WriteLine(JsonHelper.Serialize(new ErrorResponse("not_initialized")));
            return 1;
        }

        var startTime = Stopwatch.GetTimestamp();
        var iterations = new List<ShipIteration>();
        string? worktreePath = null;

        try
        {
            // Load spec from GitHub
            if (human) Output.Header($"Ship #{issueNumber}");

            var spec = await LoadSpec(issueNumber, human);
            if (spec == null) return 1;

            if (human)
            {
                Output.Field("Title", spec.Title);
                Output.Field("Type", spec.Type);
                Output.Blank();
            }

            // Load context
            var context = await LoadContext();

            // Load domain guidance
            var domain = ExtractDomain(spec.Body);
            var domainGuidance = await LoadDomainGuidance(domain);

            // Extract test command from spec
            var testCommand = ExtractTestCommand(spec.Body);

            // Create worktree
            worktreePath = await CreateWorktree(issueNumber, human);
            if (worktreePath == null) return 1;

            if (human)
            {
                Output.Info($"Worktree: {worktreePath}");
                Output.Info($"Branch: issue-{issueNumber}");
                Output.Blank();
            }

            // Work loop
            string? rejectionFeedback = null;
            for (var i = 1; i <= MaxIterations; i++)
            {
                var iterStart = Stopwatch.GetTimestamp();

                if (human) Output.Info($"Iteration {i}/{MaxIterations}...");

                // Build and invoke work prompt
                var prompt = BuildWorkPrompt(i, MaxIterations, issueNumber, spec, context, domainGuidance, rejectionFeedback);
                var (output, error) = await InvokeClaude(prompt, worktreePath, ClaudeTimeoutMs);

                var iterDuration = GetElapsedMs(iterStart);

                if (error != null)
                {
                    iterations.Add(new ShipIteration(i, "ERROR", error, DateTime.UtcNow, iterDuration));
                    if (human) Output.ShipIteration(i, MaxIterations, "ERROR", error, iterDuration);
                    break;
                }

                var signal = ParseSignal(output);

                iterations.Add(new ShipIteration(i, signal.Signal, signal.Done ?? signal.Summary ?? signal.AbortReason, DateTime.UtcNow, iterDuration));
                if (human) Output.ShipIteration(i, MaxIterations, signal.Signal, signal.Done ?? signal.Summary ?? signal.AbortReason, iterDuration);

                switch (signal.Signal)
                {
                    case "MILL_CONTINUE":
                        rejectionFeedback = null;
                        continue;

                    case "MILL_VERIFY":
                    {
                        // Independent verification
                        if (human)
                        {
                            Output.Blank();
                            Output.Info("Running independent verification...");
                        }

                        var verifyStart = Stopwatch.GetTimestamp();
                        var verifyPrompt = BuildVerifyPrompt(spec, testCommand, context);
                        var (verifyOutput, verifyError) = await InvokeClaude(verifyPrompt, worktreePath, ClaudeTimeoutMs);
                        var verifyDuration = GetElapsedMs(verifyStart);

                        if (verifyError != null)
                        {
                            iterations.Add(new ShipIteration(i, "VERIFY_ERROR", verifyError, DateTime.UtcNow, verifyDuration));
                            if (human) Output.ShipIteration(i, MaxIterations, "VERIFY_ERROR", verifyError, verifyDuration);
                            break;
                        }

                        var verifySignal = ParseSignal(verifyOutput);
                        iterations.Add(new ShipIteration(i, verifySignal.Signal, verifySignal.Suggestion, DateTime.UtcNow, verifyDuration));
                        if (human) Output.ShipIteration(i, MaxIterations, verifySignal.Signal, verifySignal.Suggestion ?? "Verification complete", verifyDuration);

                        if (verifySignal.Signal == "MILL_DONE")
                        {
                            // Push and create PR
                            var branch = signal.Branch ?? $"issue-{issueNumber}";
                            var title = signal.Title ?? $"#{issueNumber}: {spec.Title}";
                            var summary = signal.Summary ?? "";

                            var prResult = await CreatePullRequest(issueNumber, branch, title, summary, worktreePath, human);
                            var totalDuration = GetElapsedMs(startTime);

                            var result = new ShipResult(
                                Success: prResult.Success,
                                Issue: issueNumber,
                                Title: spec.Title,
                                Outcome: prResult.Success ? "success" : "pr_failed",
                                Iterations: iterations,
                                DurationMs: totalDuration,
                                PrUrl: prResult.Url,
                                PrNumber: prResult.Number
                            );

                            await RecordHistory(result, spec);

                            if (human)
                                Output.ShipSummary(prResult.Success, iterations.Count, totalDuration, prResult.Url);
                            else
                                Console.WriteLine(JsonHelper.Serialize(result));

                            return prResult.Success ? 0 : 1;
                        }
                        else if (verifySignal.Signal == "MILL_REJECTED")
                        {
                            rejectionFeedback = FormatRejectionFeedback(verifySignal);
                            if (human) Output.Warn("Verification rejected — iterating with feedback");
                            continue;
                        }
                        break;
                    }

                    case "MILL_ABORT":
                    {
                        var totalDuration = GetElapsedMs(startTime);
                        var result = new ShipResult(
                            Success: false,
                            Issue: issueNumber,
                            Title: spec.Title,
                            Outcome: "aborted",
                            Iterations: iterations,
                            DurationMs: totalDuration,
                            AbortReason: signal.AbortReason
                        );

                        await RecordHistory(result, spec);

                        if (human)
                            Output.ShipSummary(false, iterations.Count, totalDuration, null);
                        else
                            Console.WriteLine(JsonHelper.Serialize(result));

                        return 1;
                    }

                    default:
                        if (human) Output.Warn($"Unknown signal: {signal.Signal}");
                        continue;
                }
            }

            // Max iterations reached
            var finalDuration = GetElapsedMs(startTime);
            var abandoned = new ShipResult(
                Success: false,
                Issue: issueNumber,
                Title: spec.Title,
                Outcome: "max_iterations",
                Iterations: iterations,
                DurationMs: finalDuration
            );

            await RecordHistory(abandoned, spec);

            if (human)
                Output.ShipSummary(false, iterations.Count, finalDuration, null);
            else
                Console.WriteLine(JsonHelper.Serialize(abandoned));

            return 1;
        }
        finally
        {
            if (worktreePath != null)
                await CleanupWorktree(issueNumber, human);
        }
    }

    private static int ShowHelp()
    {
        Console.Error.WriteLine("""
            usage: mill ship <issue-number> [--human]

            Implements a spec through bounded iterations:
              1. Creates a worktree for the issue
              2. Iterates: implement slice → test → commit → signal
              3. Independent verification on MILL_VERIFY
              4. Creates PR on success

            options:
              <issue-number>          GitHub issue number to implement
            """);
        return 1;
    }

    // ── Spec Loading ──────────────────────────────────────────────────

    private static async Task<SpecDetail?> LoadSpec(int issueNumber, bool human)
    {
        var result = await RunProcess("gh", $"issue view {issueNumber} --json number,title,body,labels,state,createdAt", ProjectContext.ProjectPath);

        if (!result.Success)
        {
            if (human)
                Console.Error.WriteLine($"Failed to load spec #{issueNumber}: {result.Error}");
            else
                Console.WriteLine(JsonHelper.Serialize(new ErrorResponse($"spec_load_failed: {result.Error}")));
            return null;
        }

        var ghIssue = JsonHelper.Deserialize<GhIssueViewItem>(result.Output);
        if (ghIssue == null)
        {
            if (human)
                Console.Error.WriteLine($"Failed to parse issue #{issueNumber}");
            else
                Console.WriteLine(JsonHelper.Serialize(new ErrorResponse("spec_parse_failed")));
            return null;
        }

        return new SpecDetail(
            Number: ghIssue.Number,
            Title: ghIssue.Title,
            Body: ghIssue.Body ?? "",
            Type: ExtractType(ghIssue.Labels),
            Status: ghIssue.State.Equals("open", StringComparison.OrdinalIgnoreCase) ? "open" : "closed",
            Persona: ExtractPersona(ghIssue.Labels),
            Labels: ghIssue.Labels?.Select(l => l.Name).ToList() ?? [],
            CreatedAt: ghIssue.CreatedAt
        );
    }

    // ── Worktree Management ───────────────────────────────────────────

    private static async Task<string?> CreateWorktree(int issueNumber, bool human)
    {
        var worktreePath = Path.Combine(WorkPath, $"issue-{issueNumber}");
        var branch = $"issue-{issueNumber}";

        // Clean up stale worktree if it exists
        if (Directory.Exists(worktreePath))
        {
            await RunProcess("git", $"worktree remove --force \"{worktreePath}\"", ProjectContext.ProjectPath);
        }

        // Try to create worktree with existing branch first, then new branch
        var result = await RunProcess("git", $"worktree add \"{worktreePath}\" {branch}", ProjectContext.ProjectPath);
        if (!result.Success)
        {
            result = await RunProcess("git", $"worktree add -b {branch} \"{worktreePath}\"", ProjectContext.ProjectPath);
        }

        if (!result.Success)
        {
            if (human)
                Console.Error.WriteLine($"Failed to create worktree: {result.Error}");
            else
                Console.WriteLine(JsonHelper.Serialize(new ErrorResponse($"worktree_failed: {result.Error}")));
            return null;
        }

        // Inherit context.md from parent
        if (File.Exists(ContextPath))
        {
            var worktreeMillDir = Path.Combine(worktreePath, ".mill");
            Directory.CreateDirectory(worktreeMillDir);
            File.Copy(ContextPath, Path.Combine(worktreeMillDir, "context.md"), overwrite: true);
        }

        // Ensure ship work directory exists in worktree
        Directory.CreateDirectory(Path.Combine(worktreePath, ".mill", "ship", "work"));
        Directory.CreateDirectory(Path.Combine(worktreePath, ".mill", "observations"));

        return worktreePath;
    }

    private static async Task CleanupWorktree(int issueNumber, bool human)
    {
        var worktreePath = Path.Combine(WorkPath, $"issue-{issueNumber}");
        if (Directory.Exists(worktreePath))
        {
            var result = await RunProcess("git", $"worktree remove --force \"{worktreePath}\"", ProjectContext.ProjectPath);
            if (human && !result.Success)
                Output.Warn($"Worktree cleanup failed: {result.Error}");
        }
    }

    // ── Claude Invocation ─────────────────────────────────────────────

    private static async Task<(string Output, string? Error)> InvokeClaude(string prompt, string workingDir, int timeoutMs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "claude",
            Arguments = "-p --output-format json --allowedTools \"Read,Edit,Write,Glob,Grep,Bash\"",
            WorkingDirectory = workingDir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
                return ("", "Failed to start claude");

            // Write prompt via stdin to bypass Windows 32k command-line limit
            await process.StandardInput.WriteAsync(prompt);
            process.StandardInput.Close();

            using var cts = new CancellationTokenSource(timeoutMs);
            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return ("", "Claude invocation timed out");
            }

            var output = await outputTask;
            var stderr = await errorTask;

            if (process.ExitCode != 0)
                return ("", $"Claude exited with code {process.ExitCode}: {stderr.Trim()}");

            // Unwrap JSON envelope
            var unwrapped = UnwrapClaudeResponse(output);
            return (unwrapped ?? output, null);
        }
        catch (Exception ex)
        {
            return ("", $"Claude invocation failed: {ex.Message}");
        }
    }

    private static string? UnwrapClaudeResponse(string json)
    {
        try
        {
            var response = JsonHelper.Deserialize<ClaudeResponse>(json.Trim());
            if (response == null) return null;
            if (response.IsError) return null;
            return response.Result;
        }
        catch
        {
            return null;
        }
    }

    // ── Signal Parsing ────────────────────────────────────────────────

    private static ParsedSignal ParseSignal(string output)
    {
        // Scan for MILL_ABORT first (uses colon syntax, not JSON)
        var abortMatch = Regex.Match(output, @"MILL_ABORT:\s*(.+?)(?:\n|$)", RegexOptions.Singleline);
        if (abortMatch.Success)
        {
            return new ParsedSignal("MILL_ABORT", AbortReason: abortMatch.Groups[1].Value.Trim());
        }

        // Scan for MILL_DONE (no payload)
        if (output.Contains("MILL_DONE"))
        {
            return new ParsedSignal("MILL_DONE");
        }

        // Scan for MILL_VERIFY + JSON payload
        var verifyMatch = Regex.Match(output, @"MILL_VERIFY\s*\n\s*(\{[\s\S]*?\})", RegexOptions.Singleline);
        if (verifyMatch.Success)
        {
            var payload = TryDeserialize<VerifyPayload>(verifyMatch.Groups[1].Value);
            if (payload != null)
            {
                return new ParsedSignal("MILL_VERIFY",
                    Branch: payload.Branch,
                    Title: payload.Title,
                    Summary: payload.Summary,
                    Done: payload.Done,
                    Verification: payload.Verification);
            }
            return new ParsedSignal("MILL_VERIFY");
        }

        // Scan for MILL_REJECTED + JSON payload
        var rejectedMatch = Regex.Match(output, @"MILL_REJECTED\s*\n\s*(\{[\s\S]*?\})", RegexOptions.Singleline);
        if (rejectedMatch.Success)
        {
            var payload = TryDeserialize<RejectedPayload>(rejectedMatch.Groups[1].Value);
            if (payload != null)
            {
                return new ParsedSignal("MILL_REJECTED",
                    Blockers: payload.Blockers,
                    Suggestion: payload.Suggestion);
            }
            return new ParsedSignal("MILL_REJECTED");
        }

        // Scan for MILL_CONTINUE + JSON payload
        var continueMatch = Regex.Match(output, @"MILL_CONTINUE\s*\n\s*(\{[\s\S]*?\})", RegexOptions.Singleline);
        if (continueMatch.Success)
        {
            var payload = TryDeserialize<ContinuePayload>(continueMatch.Groups[1].Value);
            if (payload != null)
            {
                return new ParsedSignal("MILL_CONTINUE", Done: payload.Done, Next: payload.Next);
            }
            return new ParsedSignal("MILL_CONTINUE");
        }

        // No recognized signal
        return new ParsedSignal("UNKNOWN");
    }

    private static T? TryDeserialize<T>(string json)
    {
        try { return JsonHelper.Deserialize<T>(json); }
        catch { return default; }
    }

    // ── Prompt Building ───────────────────────────────────────────────

    private static string BuildWorkPrompt(int iteration, int max, int issueNumber, SpecDetail spec, string context, string domainGuidance, string? rejectionFeedback)
    {
        var template = LoadPromptTemplate("ship-iterate.md");

        var rejectionContext = "";
        if (!string.IsNullOrEmpty(rejectionFeedback))
        {
            rejectionContext = $"""

                ## Rejection Feedback (from previous verification)

                {rejectionFeedback}

                Address ALL blockers before signaling MILL_VERIFY again.
                """;
        }

        return template
            .Replace("{{ITERATION}}", iteration.ToString())
            .Replace("{{MAX_ITERATIONS}}", max.ToString())
            .Replace("{{ISSUE_NUMBER}}", issueNumber.ToString())
            .Replace("{{SPEC_REF}}", spec.Title)
            .Replace("{{SPEC_CONTENT}}", spec.Body)
            .Replace("{{CONTEXT}}", context)
            .Replace("{{DOMAIN_GUIDANCE}}", domainGuidance)
            .Replace("{{REJECTION_CONTEXT}}", rejectionContext);
    }

    private static string BuildVerifyPrompt(SpecDetail spec, string testCommand, string context)
    {
        var template = LoadPromptTemplate("ship-verify.md");

        return template
            .Replace("{{SPEC_CONTENT}}", spec.Body)
            .Replace("{{TEST_COMMAND}}", testCommand)
            .Replace("{{CONTEXT}}", context);
    }

    private static string LoadPromptTemplate(string name)
    {
        var path = Path.Combine(MillPaths.Templates, "prompts", name);
        if (File.Exists(path))
            return File.ReadAllText(path);

        // Fallback: minimal template
        return name switch
        {
            "ship-iterate.md" => "Implement the spec. Signal MILL_CONTINUE, MILL_VERIFY, or MILL_ABORT when done.\n\n{{SPEC_CONTENT}}",
            "ship-verify.md" => "Verify the implementation matches the spec. Signal MILL_DONE or MILL_REJECTED.\n\n{{SPEC_CONTENT}}\n\nTest command: {{TEST_COMMAND}}",
            _ => ""
        };
    }

    // ── Context & Domain ──────────────────────────────────────────────

    private static async Task<string> LoadContext()
    {
        if (File.Exists(ContextPath))
            return await File.ReadAllTextAsync(ContextPath);
        return "No project context available.";
    }

    private static string ExtractDomain(string specBody)
    {
        var match = Regex.Match(specBody, @"(?:^|\n)\s*domain:\s*(\w+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : "backend";
    }

    private static async Task<string> LoadDomainGuidance(string domain)
    {
        // For fullstack, combine backend + application
        if (domain == "fullstack")
        {
            var backend = await LoadSingleDomain("backend");
            var app = await LoadSingleDomain("application");
            return $"{backend}\n\n{app}";
        }

        return await LoadSingleDomain(domain);
    }

    private static async Task<string> LoadSingleDomain(string domain)
    {
        var path = Path.Combine(MillPaths.Domains, $"{domain}.md");
        if (File.Exists(path))
            return await File.ReadAllTextAsync(path);
        return $"No domain guidance available for '{domain}'.";
    }

    private static string ExtractTestCommand(string specBody)
    {
        // Look for test command in Loop Contract section or similar patterns
        var match = Regex.Match(specBody, @"(?:test[_ ]?command|verify|loop[_ ]?contract)[:\s]*`([^`]+)`", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;

        // Look for common test commands in code blocks
        match = Regex.Match(specBody, @"```(?:bash|sh)?\s*\n\s*((?:dotnet test|npm test|pytest|cargo test|go test|make test)[^\n]*)\s*\n", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;

        return "echo 'No test command specified in spec'";
    }

    // ── Rejection Formatting ──────────────────────────────────────────

    private static string FormatRejectionFeedback(ParsedSignal signal)
    {
        var sb = new StringBuilder();
        if (signal.Blockers is { Count: > 0 })
        {
            sb.AppendLine("**Blockers:**");
            foreach (var blocker in signal.Blockers)
                sb.AppendLine($"- {blocker}");
        }
        if (!string.IsNullOrEmpty(signal.Suggestion))
        {
            sb.AppendLine();
            sb.AppendLine($"**Suggestion:** {signal.Suggestion}");
        }
        return sb.ToString();
    }

    // ── PR Creation ───────────────────────────────────────────────────

    private static async Task<(bool Success, string? Url, int? Number)> CreatePullRequest(
        int issueNumber, string branch, string title, string summary, string worktreePath, bool human)
    {
        // Push branch
        var pushResult = await RunProcess("git", $"push -u origin {branch}", worktreePath);
        if (!pushResult.Success)
        {
            if (human) Output.Warn($"Push failed: {pushResult.Error}");
            return (false, null, null);
        }

        // Create PR with body-file to avoid escaping issues
        var body = $"""
            ## Summary

            {summary}

            Closes #{issueNumber}
            """;

        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, body);
            var escapedTitle = title.Replace("\"", "\\\"");
            var prResult = await RunProcess("gh",
                $"pr create --title \"{escapedTitle}\" --body-file \"{tempFile}\" --head {branch}",
                worktreePath);

            if (!prResult.Success)
            {
                if (human) Output.Warn($"PR creation failed: {prResult.Error}");
                return (false, null, null);
            }

            var prUrl = prResult.Output.Trim();
            int? prNumber = null;
            if (!string.IsNullOrEmpty(prUrl))
            {
                var lastSlash = prUrl.LastIndexOf('/');
                if (lastSlash >= 0 && int.TryParse(prUrl[(lastSlash + 1)..], out var num))
                    prNumber = num;
            }

            return (true, prUrl, prNumber);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── History Recording ─────────────────────────────────────────────

    private static async Task RecordHistory(ShipResult result, SpecDetail spec)
    {
        var entry = new HistoryEntry(
            Date: DateTime.UtcNow,
            Issue: result.Issue,
            Pr: result.PrNumber,
            Title: result.Title,
            Type: spec.Type,
            Persona: spec.Persona,
            Intent: result.Title,
            Outcome: result.Outcome,
            ContextAdded: [],
            Iterations: result.Iterations.Count,
            DurationMs: result.DurationMs,
            PrUrl: result.PrUrl,
            GitUser: await GetGitUser()
        );

        var history = await LoadHistory();
        history.Add(entry);
        await SaveHistory(history);
    }

    private static async Task<List<HistoryEntry>> LoadHistory()
    {
        if (!File.Exists(HistoryPath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(HistoryPath);
            var data = JsonHelper.Deserialize<HistoryFile>(json);
            return data?.Runs ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task SaveHistory(List<HistoryEntry> history)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
        var data = new HistoryFile(history);
        var json = JsonHelper.Serialize(data, indented: true);
        await File.WriteAllTextAsync(HistoryPath, json);
    }

    // ── Process Helpers ───────────────────────────────────────────────

    private static async Task<(bool Success, string Output, string Error)> RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
                return (false, "", $"Failed to start {fileName}");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode == 0, output, error.Trim());
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

    private static async Task<string?> GetGitUser()
    {
        var result = await RunProcess("git", "config user.name", ProjectContext.ProjectPath);
        return result.Success ? result.Output.Trim() : null;
    }

    // ── Label Helpers (shared with SpecCommand) ───────────────────────

    private static readonly string[] SpecTypes = ["feature", "bug", "security", "task"];

    private static string ExtractType(List<GhLabel>? labels)
    {
        if (labels == null) return "task";
        var label = labels.FirstOrDefault(l => SpecTypes.Contains(l.Name.ToLowerInvariant()));
        return label?.Name.ToLowerInvariant() ?? "task";
    }

    private static string? ExtractPersona(List<GhLabel>? labels)
    {
        var personaLabel = labels?.FirstOrDefault(l =>
            l.Name.StartsWith("persona:", StringComparison.OrdinalIgnoreCase));
        return personaLabel?.Name.Split(':').ElementAtOrDefault(1)?.Trim();
    }

    private static long GetElapsedMs(long startTimestamp) =>
        (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
