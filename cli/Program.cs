using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

// Clean up old binary from previous update (silent, on every run)
Updater.CleanupOldBinary();

return args switch
{
    ["--version"] or ["-v"] => ShowVersion(),
    ["update", "--check"] => await RunUpdateCheck(),
    ["update"] => await RunUpdate(),
    ["init"] => await Mill.Init(),
    ["spec", ..] => await Mill.RunSpec(),
    ["personas"] => Mill.RunPersonas(),
    ["run", "--auto"] => await Mill.RunAutopick(),
    ["run", "-a"] => await Mill.RunAutopick(),
    ["run", var issue, ..] => await Mill.Execute(issue, args),
    ["run"] => Mill.ListAvailableIssues(),
    _ => ShowHelp()
};

static int ShowVersion()
{
    Console.WriteLine($"mill {Mill.Version}");
    return 0;
}

static async Task<int> RunUpdateCheck()
{
    var check = await Updater.Check();

    Console.WriteLine($"  current: {check.Current}");

    if (check.Error != null)
    {
        Out.Error($"failed to check for updates: {check.Error}");
        return 1;
    }

    Console.WriteLine($"  latest:  {check.Latest}");
    Out.Blank();

    if (check.UpdateAvailable)
    {
        Console.WriteLine("  run `mill update` to install");
    }
    else
    {
        Out.Ok($"already at latest ({check.Current})");
    }

    return 0;
}

static async Task<int> RunUpdate()
{
    var check = await Updater.Check();

    Console.WriteLine($"  current: {check.Current}");

    if (check.Error != null)
    {
        Out.Error($"failed to check for updates: {check.Error}");
        return 1;
    }

    if (!check.UpdateAvailable)
    {
        Out.Blank();
        Out.Ok($"already at latest ({check.Current})");
        return 0;
    }

    Console.WriteLine($"  latest:  {check.Latest}");
    Out.Blank();

    var (success, message) = await Updater.Apply();

    if (success)
    {
        Out.Ok(message);
        return 0;
    }
    else
    {
        Out.Error(message);
        return 1;
    }
}

static int ShowHelp()
{
    // #ffcc00 = RGB(255, 204, 0), fallback to ConsoleColor.Yellow
    var trueColor = Environment.GetEnvironmentVariable("COLORTERM") is "truecolor" or "24bit";
    if (trueColor)
    {
        Console.WriteLine("  \x1b[38;2;255;204;0m■\x1b[0m mill - turning intent into verified deliverables, continuously.");
    }
    else
    {
        Console.Write("  ");
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("■");
        Console.ForegroundColor = prev;
        Console.WriteLine(" mill - turning intent into verified deliverables, continuously.");
    }
    Console.WriteLine("""

        usage:
          mill init               initialize repo for MILL
          mill spec               create specification → GitHub issue
          mill personas           create, update, or manage user personas
          mill run                list available issues
          mill run --auto         autopick best issue (health check + scoring)
          mill run 123            execute work loop on GitHub issue
          mill update             update mill to latest version
          mill update --check     check for updates without installing
          mill --version          show version
        """);
    return 0;
}

/// <summary>
/// Standardized console output helpers. Minimal, uniform, hacker style.
/// </summary>
static class Out
{
    public static void Step(string msg)
    {
        WriteColored("  ▶ ", ConsoleColor.Cyan);
        Console.WriteLine(msg);
    }

    public static void Ok(string msg)
    {
        WriteColored("  ✓ ", ConsoleColor.Green);
        Console.WriteLine(msg);
    }

    public static void Warn(string msg)
    {
        WriteColored("  ▲ ", ConsoleColor.Yellow);
        Console.WriteLine(msg);
    }

    public static void Error(string msg)
    {
        WriteColored("  ✕ ", ConsoleColor.Red);
        Console.Error.WriteLine(msg);
    }

    public static void Detail(string msg)
    {
        WriteColored("    ↳ ", ConsoleColor.DarkGray);
        Console.WriteLine(msg);
    }

    public static void Line() => Console.WriteLine("  ---");
    public static void Blank() => Console.WriteLine();

    static void WriteColored(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }
}

static partial class Mill
{
    /// <summary>
    /// Gets the current mill version from assembly metadata.
    /// Falls back to "unknown" if not available.
    /// </summary>
    public static string Version =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion?.Split('+')[0] ?? "unknown";

    /// <summary>
    /// Gets the expected GitHub release asset name for the current platform.
    /// </summary>
    public static string AssetName
    {
        get
        {
            var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
                   : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
                   : "linux";
            var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            return os == "win" ? $"mill-{os}-{arch}.exe" : $"mill-{os}-{arch}";
        }
    }

    // Parent git root - cached at startup, never changes (used for config, standards, drafts)
    static string? _parentGitRoot;
    static string ParentGitRoot => _parentGitRoot ??= ResolveGitRoot();

    // Current git root - dynamic, can change when cd'ing to worktree (used for context, memory)
    static string CurrentGitRoot => ResolveGitRoot();

    static string ResolveGitRoot()
    {
        var (exit, output, _) = Git("rev-parse", "--show-toplevel");
        return exit == 0 ? output.Trim() : Directory.GetCurrentDirectory();
    }

    // Parent .mill/ paths - project-level, inherited by worktrees
    static string ParentMillDir => Path.Combine(ParentGitRoot, ".mill");
    static string ConfigFile => Path.Combine(ParentMillDir, "config.json");
    static string StandardsDir => Path.Combine(ParentMillDir, "standards");
    static string DraftsDir => Path.Combine(ParentMillDir, "drafts");

    // Current .mill/ paths - can be worktree-local
    static string CurrentMillDir => Path.Combine(CurrentGitRoot, ".mill");
    static string ContextFile => Path.Combine(CurrentMillDir, "context.md");
    static string MemoryDir => Path.Combine(CurrentMillDir, "memory");

    // Convenience - for init and other parent-only operations
    static string GitRoot => ParentGitRoot;
    static string MillDir => ParentMillDir;

    static readonly string[] RequiredLabels =
    [
        // Workflow labels
        "in-progress",
        "blocked",
        "ready-for-review",
        "backlog",
        // Intent type labels
        "feature",
        "bug",
        "security",
        "task"
    ];

    public static async Task<int> Init()
    {
        Out.Blank();

        // Check for existing setup
        if (File.Exists(ContextFile))
        {
            Out.Warn("existing MILL setup detected (.mill/context.md)");
            Out.Detail("continuing will regenerate all context");
            Out.Blank();
            Console.Write("  continue? [y/n] ");
            var confirm = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (confirm != "y")
            {
                Out.Blank();
                Out.Warn("aborted");
                Out.Blank();
                return 0;
            }
            Out.Blank();

            // Remove existing context to force regeneration
            File.Delete(ContextFile);
        }

        // Check git repo
        Out.Step("verifying git repo...");
        if (!IsGitRepo())
        {
            Out.Error("not in a git repository");
            return 1;
        }
        Out.Ok("git repo");

        // Check gh auth
        Out.Step("verifying gh auth...");
        var (authExit, _) = Gh("auth", "status");
        if (authExit != 0)
        {
            Out.Error("gh not authenticated — run: gh auth login");
            return 1;
        }
        Out.Ok("gh authenticated");

        // Create labels
        Out.Step("verifying labels...");
        var (_, existing) = GetExistingLabels();
        var created = 0;
        var existed = 0;

        foreach (var label in RequiredLabels)
        {
            if (existing.Contains(label))
            {
                existed++;
            }
            else
            {
                var color = label switch
                {
                    "in-progress" => "FFA500",
                    "blocked" => "D93F0B",
                    "ready-for-review" => "0E8A16",
                    "backlog" => "C5DEF5",
                    "enhancement" => "A2EEEF",
                    "bug" => "D73A4A",
                    "security" => "EE0701",
                    "chore" => "666666",
                    _ => "CCCCCC"
                };
                Gh("label", "create", label, "--color", color, "--force");
                created++;
            }
        }
        Out.Ok($"labels ({existed} exist, {created} created)");

        // Create directories
        Out.Step("creating directories...");
        Directory.CreateDirectory(MillDir);
        Directory.CreateDirectory(DraftsDir);
        Directory.CreateDirectory(StandardsDir);
        Directory.CreateDirectory(MemoryDir);
        Out.Ok(".mill/ structure");

        // Create default config if not exists
        if (!File.Exists(ConfigFile))
        {
            Out.Step("creating config...");
            WriteDefaultConfig();
            Out.Ok("config created");
        }
        else
        {
            Out.Detail("config already exists");
        }

        // Add .mill/work/ and .mill/.prompt to gitignore (ephemeral files)
        Out.Step("updating .gitignore...");
        var gitignorePath = Path.Combine(GitRoot, ".gitignore");
        var gitignore = File.Exists(gitignorePath) ? File.ReadAllText(gitignorePath) : "";
        var gitignoreUpdates = new List<string>();

        if (!gitignore.Contains(".mill/work/"))
            gitignoreUpdates.Add(".mill/work/");
        if (!gitignore.Contains(".mill/.prompt"))
            gitignoreUpdates.Add(".mill/.prompt");

        if (gitignoreUpdates.Count > 0)
        {
            File.AppendAllText(gitignorePath, "\n# MILL ephemeral\n" + string.Join("\n", gitignoreUpdates) + "\n");
            Out.Ok($"{string.Join(", ", gitignoreUpdates)} added to .gitignore");
        }
        else
        {
            Out.Detail(".gitignore already configured");
        }

        // Run context warmup (also generates AGENTS.md if missing)
        Out.Blank();
        Out.Step("building context...");
        Out.Blank();

        var warmupPrompt = Path.Combine(FindMillHome(), "spec/prompts/context-warmup.md");
        if (!File.Exists(warmupPrompt))
        {
            Out.Error($"warmup prompt not found: {warmupPrompt}");
            Out.Detail("check MILL_HOME environment variable or installation");
            return 1;
        }

        var exitCode = await RunClaudeStreaming(warmupPrompt);
        if (exitCode != 0)
        {
            Out.Blank();
            Out.Error("context warmup failed");
            return 1;
        }

        // Create CLAUDE.md shim if AGENTS.md exists but CLAUDE.md doesn't
        var agentsMd = Path.Combine(GitRoot, "AGENTS.md");
        var claudeMd = Path.Combine(GitRoot, "CLAUDE.md");
        if (File.Exists(agentsMd) && !File.Exists(claudeMd))
        {
            await File.WriteAllTextAsync(claudeMd, "@AGENTS.md\n");
            Out.Ok("CLAUDE.md shim created");
        }

        Out.Blank();
        Out.Line();
        Out.Blank();
        Out.Ok("ready — run: mill spec");
        Out.Blank();
        Out.Detail("tip: run `mill personas` to define user segments for better specs");
        Out.Blank();

        return 0;
    }

    static (bool Success, HashSet<string> Labels) GetExistingLabels()
    {
        var (exit, output) = Gh("label", "list", "--json", "name", "-q", ".[].name");
        if (exit != 0) return (false, []);
        return (true, output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToHashSet());
    }

    public static bool IsInitialized()
    {
        var (success, existing) = GetExistingLabels();
        return success && RequiredLabels.All(existing.Contains);
    }

    public static async Task<int> RunSpec()
    {
        if (!IsGitRepo())
        {
            Out.Error("not in a git repository");
            return 1;
        }

        if (!IsInitialized())
        {
            Out.Warn("mill not initialized — run: mill init");
            return 1;
        }

        var (needsWarmup, reason) = CheckContextStaleness();
        var hasUncommitted = HasUncommittedChanges();

        if (needsWarmup)
        {
            Out.Warn(reason.ToLower());
            if (hasUncommitted)
                Out.Warn("uncommitted changes (not in context)");

            Out.Blank();
            Out.Step("building context...");
            Out.Blank();

            var exitCode = await RunClaudeStreaming("spec/prompts/context-warmup.md");
            if (exitCode != 0) return exitCode;

            Out.Blank();
            Out.Line();
            Out.Blank();
        }

        Out.Ok($"context loaded ({GetContextInfo()})");
        if (hasUncommitted)
            Out.Detail("uncommitted changes read on demand");
        Out.Blank();

        // Check for existing drafts
        var selectedDraft = PromptForDraft();

        return RunClaudeInteractive("spec/prompts/spec-draft.md", selectedDraft);
    }

    public static int RunPersonas()
    {
        if (!IsGitRepo())
        {
            Out.Error("not in a git repository");
            return 1;
        }

        Out.Blank();

        var personasFile = Path.Combine(MillDir, "personas.md");
        if (File.Exists(personasFile))
        {
            Out.Ok("existing personas found");
            Out.Detail("you can update, add, or remove personas");
        }
        else
        {
            Out.Step("no personas yet — starting creation");
        }

        Out.Blank();

        return RunClaudeInteractivePersonas("spec/prompts/personas.md");
    }

    static int RunClaudeInteractivePersonas(string promptPath)
    {
        var fullPath = Path.Combine(FindMillHome(), promptPath);
        if (!File.Exists(fullPath))
        {
            Out.Error($"prompt not found: {fullPath}");
            return 1;
        }

        var cli = ResolveCli();

        // Build prompt with pre-loaded context for persona generation
        var prompt = new System.Text.StringBuilder();
        prompt.AppendLine(File.ReadAllText(fullPath));

        prompt.AppendLine("\n---\n# Pre-loaded Context\n");

        // README is critical for understanding intended users
        var readmePath = Path.Combine(GitRoot, "README.md");
        if (File.Exists(readmePath))
        {
            prompt.AppendLine("## README.md\n");
            prompt.AppendLine(File.ReadAllText(readmePath));
        }

        // AGENTS.md for project context
        var agentsPath = Path.Combine(GitRoot, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            prompt.AppendLine("\n## AGENTS.md\n");
            prompt.AppendLine(File.ReadAllText(agentsPath));
        }

        // context.md if available
        if (File.Exists(ContextFile))
        {
            prompt.AppendLine("\n## .mill/context.md\n");
            prompt.AppendLine(File.ReadAllText(ContextFile));
        }

        // Existing personas for update mode
        var personasFile = Path.Combine(MillDir, "personas.md");
        if (File.Exists(personasFile))
        {
            prompt.AppendLine("\n## Existing Personas (.mill/personas.md)\n");
            prompt.AppendLine(File.ReadAllText(personasFile));
            prompt.AppendLine("\n**Mode:** Update existing personas based on new evidence or user input.");
        }

        var promptContent = prompt.ToString();

        // Write to .mill/.prompt to avoid command line length limits (Windows: 32KB, Linux: 128KB per arg)
        var promptFile = Path.Combine(CurrentMillDir, ".prompt");
        Directory.CreateDirectory(CurrentMillDir);
        File.WriteAllText(promptFile, promptContent);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cli,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            }
        };

        process.StartInfo.ArgumentList.Add("--dangerously-skip-permissions");
        process.StartInfo.ArgumentList.Add("--append-system-prompt");
        process.StartInfo.ArgumentList.Add($"CRITICAL: Before responding, read {promptFile} for your full system context.");

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    static string? PromptForDraft()
    {
        if (!Directory.Exists(DraftsDir))
            return null;

        var drafts = Directory.GetFiles(DraftsDir, "*.md")
            .Select(ParseDraft)
            .Where(d => d != null)
            .OrderByDescending(d => d!.Updated)
            .ToList();

        if (drafts.Count == 0)
            return null;

        Console.WriteLine("  drafts:");
        for (var i = 0; i < drafts.Count; i++)
        {
            var d = drafts[i]!;
            var progress = $"{d.FieldsComplete}/{d.FieldsComplete + d.FieldsPending}";
            Console.WriteLine($"    {i + 1}. {d.Title} ({d.Type}, {progress})");
        }
        Out.Blank();
        Console.Write($"  [1-{drafts.Count}] or enter for new: ");

        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
            return null;

        if (int.TryParse(input, out var choice) && choice >= 1 && choice <= drafts.Count)
            return drafts[choice - 1]!.Path;

        return null;
    }

    static DraftInfo? ParseDraft(string path)
    {
        try
        {
            var lines = File.ReadLines(path).Take(20).ToList();
            if (lines.Count < 3 || lines[0] != "---")
                return null;

            var endIdx = lines.Skip(1).ToList().FindIndex(l => l == "---");
            if (endIdx < 0) return null;

            var yaml = lines.Skip(1).Take(endIdx).ToList();
            string? type = null, title = null, status = null;
            DateTime updated = File.GetLastWriteTime(path);
            int complete = 0, pending = 0;

            foreach (var line in yaml)
            {
                if (line.StartsWith("type:")) type = line[5..].Trim();
                else if (line.StartsWith("title:")) title = line[6..].Trim();
                else if (line.StartsWith("status:")) status = line[7..].Trim();
                else if (line.StartsWith("updated:") && DateTime.TryParse(line[8..].Trim(), out var u)) updated = u;
                else if (line.StartsWith("fields_complete:")) complete = CountYamlList(lines, yaml.IndexOf(line) + 1);
                else if (line.StartsWith("fields_pending:")) pending = CountYamlList(lines, yaml.IndexOf(line) + 1);
            }

            if (type == null || title == null) return null;

            return new DraftInfo(path, type, title, status ?? "unknown", updated, complete, pending);
        }
        catch { return null; }
    }

    static int CountYamlList(List<string> lines, int startIdx)
    {
        var count = 0;
        for (var i = startIdx + 1; i < lines.Count && lines[i].StartsWith("  - "); i++)
            count++;
        return count;
    }

    record DraftInfo(string Path, string Type, string Title, string Status, DateTime Updated, int FieldsComplete, int FieldsPending);

    public static async Task<int> Execute(string issue, string[] args)
    {
        if (!IsInitialized())
        {
            Out.Warn("mill not initialized — run: mill init");
            return 1;
        }

        var maxIterations = int.TryParse(Environment.GetEnvironmentVariable("MILL_MAX_ITERATIONS"), out var n) ? n : 20;
        const string verifyToken = "MILL_VERIFY";
        const string doneToken = "MILL_DONE";
        const string failedToken = "MILL_REJECTED";

        // Check if issue is a GitHub issue number
        var issueNumber = issue;
        var isGhIssue = int.TryParse(issueNumber, out _);

        string specContent;
        string specRef;

        if (isGhIssue)
        {
            Out.Step($"fetching issue #{issueNumber}...");
            var (exitCode, output) = Gh("issue", "view", issueNumber, "--json", "body,state,labels");
            if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                Out.Error($"failed to fetch issue #{issueNumber}");
                return 1;
            }

            var issueDetail = JsonSerializer.Deserialize(output, GhJsonContext.Default.GhIssueDetail);
            if (issueDetail == null)
            {
                Out.Error("failed to parse issue");
                return 1;
            }

            // Guard: check issue state and labels
            if (issueDetail.State.Equals("CLOSED", StringComparison.OrdinalIgnoreCase))
            {
                Out.Warn("issue is closed — work was merged");
                return 0;
            }

            var labels = issueDetail.Labels.Select(l => l.Name.ToLowerInvariant()).ToList();
            if (labels.Contains("ready-for-review"))
            {
                Out.Warn("issue has 'ready-for-review' — remove label to re-run");
                return 0;
            }
            if (labels.Contains("in-progress"))
            {
                Out.Warn("issue has 'in-progress' — already running?");
                return 0;
            }
            if (labels.Contains("blocked"))
            {
                Out.Warn("issue has 'blocked' — resolve before running");
                return 0;
            }

            specContent = issueDetail.Body?.Trim() ?? "";
            specRef = $"#{issueNumber}";
            Out.Ok($"loaded issue #{issueNumber}");

            // Add in-progress label
            Out.Step("marking in-progress...");
            Gh("issue", "edit", issueNumber, "--remove-label", "ready-for-review,blocked", "--add-label", "in-progress");
        }
        else
        {
            // Fallback: local file (for offline/gh-unavailable scenarios)
            if (!File.Exists(issue))
            {
                Out.Error($"spec not found: {issue}");
                return 1;
            }
            specContent = await File.ReadAllTextAsync(issue);
            specRef = issue;
        }

        // Validate prompts exist before starting
        var iterationPrompt = Path.Combine(FindMillHome(), "run/prompts/loop-iterate.md");
        var verifyPrompt = Path.Combine(FindMillHome(), "run/prompts/loop-verify.md");
        if (!File.Exists(iterationPrompt))
        {
            Out.Error($"iteration prompt not found: {iterationPrompt}");
            Out.Detail("check MILL_HOME environment variable or installation");
            return 1;
        }
        if (!File.Exists(verifyPrompt))
        {
            Out.Error($"verify prompt not found: {verifyPrompt}");
            Out.Detail("check MILL_HOME environment variable or installation");
            return 1;
        }

        // Create isolated worktree for execution
        var worktreeName = isGhIssue ? $"issue-{issueNumber}" : $"spec-{Path.GetFileNameWithoutExtension(issue)}";
        var workDir = Path.Combine(MillDir, "work");
        var worktreePath = Path.Combine(workDir, worktreeName);
        var originalDir = Directory.GetCurrentDirectory();

        Out.Step($"creating worktree {worktreePath}...");

        // Ensure .mill/work directory exists
        Directory.CreateDirectory(workDir);

        // Remove existing worktree if present (from crashed run)
        if (Directory.Exists(worktreePath))
        {
            var (rmExit, _, rmErr) = Git("worktree", "remove", "--force", worktreePath);
            if (rmExit != 0)
                Out.Warn($"failed to remove stale worktree: {rmErr.Trim()}");
        }

        var branchName = $"issue-{issueNumber}";

        // Delete existing branch if present (from previous run)
        Git("branch", "-D", branchName);
        var (wtExit, _, wtErr) = Git("worktree", "add", "-b", branchName, worktreePath, "HEAD");
        if (wtExit != 0)
        {
            Out.Error($"failed to create worktree: {wtErr.Trim()}");
            return 1;
        }
        Out.Ok($"worktree created (branch: {branchName})");

        var result = 1;
        try
        {
            // Change to worktree directory
            Directory.SetCurrentDirectory(Path.Combine(originalDir, worktreePath));

            // Ensure context exists in worktree (uses CurrentMillDir which resolves to worktree)
            var (needsWarmup, reason) = CheckContextStaleness();
            if (needsWarmup)
            {
                Out.Warn(reason.ToLower());
                Out.Step("building context...");
                Out.Blank();

                // Ensure worktree's .mill directory exists (may not if .mill/ wasn't committed)
                Directory.CreateDirectory(CurrentMillDir);

                var warmupPrompt = Path.Combine(FindMillHome(), "spec/prompts/context-warmup.md");
                var warmupExit = await RunClaudeStreaming(warmupPrompt);
                if (warmupExit != 0)
                {
                    Out.Error("warmup failed");
                    return 1;
                }
                Out.Blank();
                Out.Ok("context ready");
            }

            string? lastFailureContext = null;

            for (var i = 0; i < maxIterations; i++)
            {
                Out.Blank();
                Out.Step($"iteration {i + 1}/{maxIterations}");
                Out.Blank();

                var template = await File.ReadAllTextAsync(iterationPrompt);
                var rendered = template
                    .Replace("{{SPEC_CONTENT}}", specContent + (lastFailureContext != null ? $"\n\n---\n## Previous Verification Failure\n{lastFailureContext}" : ""))
                    .Replace("{{SPEC_REF}}", specRef)
                    .Replace("{{ISSUE_NUMBER}}", isGhIssue ? issueNumber : "")
                    .Replace("{{ITERATION}}", (i + 1).ToString())
                    .Replace("{{MAX_ITERATIONS}}", maxIterations.ToString())
                    .Replace("{{COMPLETION_TOKEN}}", verifyToken);

                var output = await RunClaudeBatch(rendered);

                if (output.Contains("MILL_CONTINUE"))
                {
                    // Slice complete, more work remains
                    var nextSlice = ExtractNextSlice(output);
                    Out.Blank();
                    Out.Ok("slice complete");
                    if (nextSlice != null)
                        Out.Detail($"next: {nextSlice}");
                    continue;
                }

                if (output.Contains(verifyToken))
                {
                    // Parse metadata from MILL_VERIFY output
                    var metadata = ParseVerifyMetadata(output);
                    if (metadata == null)
                    {
                        Out.Warn("invalid verify metadata, continuing iteration");
                        continue;
                    }

                    // Extract test command from spec
                    var testCommand = ExtractTestCommand(specContent);

                    // Run verification prompt
                    Out.Blank();
                    Out.Step("running verification...");
                    Out.Blank();

                    var verifyTemplate = await File.ReadAllTextAsync(verifyPrompt);
                    var verifyRendered = verifyTemplate
                        .Replace("{{SPEC_CONTENT}}", specContent)
                        .Replace("{{SPEC_REF}}", specRef)
                        .Replace("{{ISSUE_NUMBER}}", isGhIssue ? issueNumber : "")
                        .Replace("{{TEST_COMMAND}}", testCommand ?? "echo 'no test command specified'");

                    var verifyOutput = await RunClaudeBatch(verifyRendered);

                    if (verifyOutput.Contains(doneToken))
                    {
                        // Verification passed - create PR
                        Out.Blank();
                        Out.Ok("verification passed");

                        if (isGhIssue)
                        {
                            // Push branch
                            Out.Step($"pushing branch {metadata.Branch}...");
                            var (pushExit, _, pushErr) = Run("git", "push", "-u", "origin", metadata.Branch);
                            if (pushExit != 0)
                            {
                                Out.Warn($"push failed: {pushErr.Trim()}");
                                lastFailureContext = $"Push failed: {pushErr.Trim()}";
                                continue;
                            }

                            // Create PR
                            Out.Step("creating PR...");
                            var prBody = $"Fixes #{issueNumber}\n\n## Summary\n{metadata.Summary}\n\n## Verification\n{metadata.Verification}";
                            var (prExit, prOut, prErr) = Run("gh", "pr", "create", "--title", metadata.Title, "--body", prBody);
                            if (prExit != 0)
                            {
                                Out.Warn($"PR creation failed: {prErr.Trim()}");
                                lastFailureContext = $"PR creation failed: {prErr.Trim()}";
                                continue;
                            }
                            Out.Detail(prOut.Trim());

                            // Mark ready-for-review
                            Out.Step("marking ready-for-review...");
                            Run("gh", "issue", "edit", issueNumber, "--remove-label", "in-progress", "--add-label", "ready-for-review");
                        }

                        Out.Blank();
                        Out.Ok(doneToken);
                        result = 0;
                        break;
                    }
                    else if (verifyOutput.Contains(failedToken))
                    {
                        // Verification failed - extract reason, continue iteration
                        var failure = ParseVerifyFailure(verifyOutput);
                        Out.Blank();
                        Out.Warn("verification failed");
                        if (failure != null)
                        {
                            Out.Detail(failure.Suggestion ?? string.Join(", ", failure.Blockers));
                            lastFailureContext = $"Blockers: {string.Join("; ", failure.Blockers)}\nSuggestion: {failure.Suggestion}";
                        }
                        continue;
                    }
                    else
                    {
                        Out.Warn("verify prompt did not output expected token");
                        continue;
                    }
                }
                else
                {
                    // No recognized signal - warn and continue
                    Out.Blank();
                    Out.Warn("no signal (expected MILL_CONTINUE or MILL_VERIFY)");
                    continue;
                }
            }

            if (result != 0)
            {
                Out.Blank();
                Out.Warn($"max iterations ({maxIterations}) reached");
            }
        }
        finally
        {
            // Always clean up: return to original directory and remove worktree
            Directory.SetCurrentDirectory(originalDir);

            Out.Step("cleaning up worktree...");
            var (cleanExit, _, cleanErr) = Git("worktree", "remove", "--force", worktreePath);
            if (cleanExit != 0)
                Out.Warn($"worktree cleanup failed: {cleanErr.Trim()}");
            else
                Out.Ok("done");
        }

        return result;
    }

    public static int ListAvailableIssues()
    {
        if (!IsInitialized())
        {
            Out.Warn("mill not initialized — run: mill init");
            return 1;
        }

        // Get repo info for URL
        var (repoExit, repoUrl) = Gh("repo", "view", "--json", "url", "-q", ".url");
        if (repoExit != 0)
        {
            Out.Error("failed to get repository info");
            return 1;
        }

        // Fetch open issues with labels
        var (exitCode, output) = Gh("issue", "list", "--state", "open", "--json", "number,title,labels,createdAt", "--limit", "100");
        if (exitCode != 0)
        {
            Out.Error("failed to fetch issues");
            return 1;
        }

        var issues = JsonSerializer.Deserialize(output, GhJsonContext.Default.ListGhIssue) ?? [];

        // Filter out workflow labels and deferred issues
        var excludeLabels = new[] { "in-progress", "blocked", "ready-for-review", "backlog", "deferred", "on-hold" };
        var available = issues
            .Where(i => !i.Labels.Any(l => excludeLabels.Contains(l.Name, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        if (available.Count == 0)
        {
            Out.Warn("no available issues");
            return 0;
        }

        // Sort: impact:high → unlabeled → impact:low, oldest first within each tier
        var sorted = available
            .OrderBy(i => GetImpactTier(i.Labels))
            .ThenBy(i => i.CreatedAt)
            .ToList();

        Out.Blank();
        Console.WriteLine("  available:");
        Out.Blank();

        var shown = sorted.Take(10).ToList();
        foreach (var issue in shown)
        {
            var impact = GetImpactLabel(issue.Labels);
            var age = FormatAge(issue.CreatedAt);
            var impactCol = impact != null ? $"[{impact}]".PadRight(7) : "       ";
            var title = issue.Title.Length > 40 ? issue.Title[..37] + "..." : issue.Title;
            Console.WriteLine($"  #{issue.Number,-4} {impactCol} {title,-40} {age}");
        }

        var remaining = sorted.Count - 10;
        if (remaining > 0)
        {
            Out.Blank();
            var filterQuery = Uri.EscapeDataString("is:open -label:in-progress -label:blocked -label:ready-for-review -label:backlog -label:deferred -label:on-hold");
            Console.WriteLine($"  +{remaining} more: {repoUrl.Trim()}/issues?q={filterQuery}");
        }

        Out.Blank();
        Console.WriteLine("  mill run <number>");
        Console.WriteLine("  mill run --auto");
        Out.Blank();

        return 0;
    }

    public static async Task<int> RunAutopick()
    {
        if (!IsInitialized())
        {
            Out.Warn("mill not initialized — run: mill init");
            return 1;
        }

        Out.Blank();
        Out.Step("checking health and scoring issues...");
        Out.Blank();

        // Load config
        var config = LoadConfig();
        var configJson = JsonSerializer.Serialize(config, MillConfigContext.Default.MillConfig);

        // Fetch open issues
        var (issuesExit, issuesJson) = Gh("issue", "list", "--state", "open", "--json", "number,title,body,labels,createdAt", "--limit", "100");
        if (issuesExit != 0)
        {
            Out.Error("failed to fetch issues");
            return 1;
        }

        // Load and render the prompt
        var promptPath = Path.Combine(FindMillHome(), "run/prompts/run-autopick.md");
        if (!File.Exists(promptPath))
        {
            Out.Error($"autopick prompt not found: {promptPath}");
            return 1;
        }

        var template = await File.ReadAllTextAsync(promptPath);
        var rendered = template
            .Replace("{{CONFIG}}", configJson)
            .Replace("{{OPEN_ISSUES}}", issuesJson);

        // Run Claude and capture output
        var output = await RunClaudeBatch(rendered);

        // Parse output for tokens
        if (output.Contains("BLOCKED"))
        {
            Out.Blank();
            Out.Warn("health check failed — resolve issues before picking new work");
            return 1;
        }

        if (output.Contains("EMPTY"))
        {
            Out.Blank();
            Out.Warn("no open issues — run: mill spec");
            return 0;
        }

        if (output.Contains("FULL"))
        {
            Out.Blank();
            Out.Warn("all issues have workflow labels — finish existing work first");
            return 0;
        }

        if (output.Contains("SKIP"))
        {
            Out.Blank();
            Out.Ok("skipped");
            return 0;
        }

        // Look for PICK:N pattern
        var pickMatch = PickPattern().Match(output);
        if (pickMatch.Success)
        {
            var issueNumber = pickMatch.Groups[1].Value;
            Out.Blank();
            Out.Ok($"selected: {issueNumber}");
            Out.Blank();

            // Chain to Execute
            return await Execute(issueNumber, [issueNumber]);
        }

        // No recognized token — assume user declined or something went wrong
        Out.Blank();
        Out.Warn("no issue selected");
        return 0;
    }

    static void WriteDefaultConfig()
    {
        var config = new MillConfig();
        var json = JsonSerializer.Serialize(config, MillConfigContext.Default.MillConfig);
        File.WriteAllText(ConfigFile, json);
    }

    static MillConfig LoadConfig()
    {
        if (!File.Exists(ConfigFile))
            return new MillConfig();

        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize(json, MillConfigContext.Default.MillConfig) ?? new MillConfig();
        }
        catch
        {
            return new MillConfig();
        }
    }

    static int GetImpactTier(List<GhLabel> labels)
    {
        if (labels.Any(l => l.Name.Equals("impact:high", StringComparison.OrdinalIgnoreCase))) return 0;
        if (labels.Any(l => l.Name.Equals("impact:low", StringComparison.OrdinalIgnoreCase))) return 2;
        return 1; // unlabeled = middle tier
    }

    static string? GetImpactLabel(List<GhLabel> labels)
    {
        if (labels.Any(l => l.Name.Equals("impact:high", StringComparison.OrdinalIgnoreCase))) return "high";
        if (labels.Any(l => l.Name.Equals("impact:low", StringComparison.OrdinalIgnoreCase))) return "low";
        return null;
    }

    static string FormatAge(DateTime created)
    {
        var age = DateTime.UtcNow - created;
        if (age.TotalDays >= 1) return $"{(int)age.TotalDays}d ago";
        if (age.TotalHours >= 1) return $"{(int)age.TotalHours}h ago";
        return $"{(int)age.TotalMinutes}m ago";
    }

    static (int ExitCode, string Output, string Error) Run(string exe, params string[] args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exe,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }

    static (int ExitCode, string Output) Gh(params string[] args)
    {
        var (exit, output, _) = Run("gh", args);
        return (exit, output);
    }

    static bool IsGitRepo() => Git("rev-parse", "--git-dir").ExitCode == 0;

    static (bool NeedsWarmup, string Reason) CheckContextStaleness()
    {
        if (!File.Exists(ContextFile))
            return (true, "Context file missing");

        var firstLine = File.ReadLines(ContextFile).FirstOrDefault() ?? "";
        var match = HashPattern().Match(firstLine);

        if (!match.Success)
            return (true, "Context missing hash");

        var contextHash = match.Groups[1].Value;
        var currentHash = Git("rev-parse", "HEAD").Output.Trim();

        return contextHash != currentHash
            ? (true, "Context stale")
            : (false, "");
    }

    static bool HasUncommittedChanges() =>
        !string.IsNullOrWhiteSpace(Git("status", "--porcelain").Output);

    static string GetContextInfo()
    {
        if (!File.Exists(ContextFile))
            return "no context";

        var firstLine = File.ReadLines(ContextFile).FirstOrDefault() ?? "";
        var match = HashPattern().Match(firstLine);

        if (!match.Success)
            return "unknown commit";

        var hash = match.Groups[1].Value[..7]; // Short hash
        return $"commit {hash}";
    }

    /// <summary>
    /// Resolve CLI executable, handling Windows .cmd wrappers.
    /// </summary>
    static string ResolveCli()
    {
        var cli = Environment.GetEnvironmentVariable("MILL_CLI") ?? "claude";

        // On Windows, npm installs create .cmd wrappers
        if (OperatingSystem.IsWindows() && !cli.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) && !cli.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            // Check if .cmd version exists in PATH
            var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];
            foreach (var dir in pathDirs)
            {
                var cmdPath = Path.Combine(dir, cli + ".cmd");
                if (File.Exists(cmdPath))
                    return cmdPath;
            }
        }

        return cli;
    }

    /// <summary>
    /// Run Claude in streaming mode - output goes directly to console.
    /// Used for warmup where we want to see progress but don't need interactivity.
    /// </summary>
    static async Task<int> RunClaudeStreaming(string promptPath)
    {
        var fullPath = Path.Combine(FindMillHome(), promptPath);
        if (!File.Exists(fullPath))
        {
            Out.Error($"prompt not found: {fullPath}");
            return 1;
        }

        var template = await File.ReadAllTextAsync(fullPath);
        var rendered = template.Replace("{{USER_PROMPT}}", "");
        var cli = ResolveCli();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cli,
                Arguments = "-p --dangerously-skip-permissions",
                RedirectStandardInput = true,
                RedirectStandardOutput = false,  // Output goes directly to console
                RedirectStandardError = false,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.StandardInput.WriteAsync(rendered);
        process.StandardInput.Close();
        await process.WaitForExitAsync();

        return process.ExitCode;
    }

    /// <summary>
    /// Run Claude in fully interactive mode - hands off to Claude completely.
    /// Pre-loads context into system prompt so it's available before user speaks.
    /// </summary>
    static int RunClaudeInteractive(string promptPath, string? draftPath = null)
    {
        var fullPath = Path.Combine(FindMillHome(), promptPath);
        if (!File.Exists(fullPath))
        {
            Out.Error($"prompt not found: {fullPath}");
            return 1;
        }

        var cli = ResolveCli();

        // Build prompt with pre-loaded context
        var prompt = new System.Text.StringBuilder();
        prompt.AppendLine(File.ReadAllText(fullPath)
            .Replace("{{USER_PROMPT}}", "")
            .Replace("{{DRAFT_PATH}}", draftPath ?? ""));

        // Pre-load context files into prompt
        prompt.AppendLine("\n---\n# Pre-loaded Context\n");

        if (File.Exists(ContextFile))
        {
            prompt.AppendLine("## .mill/context.md\n");
            prompt.AppendLine(File.ReadAllText(ContextFile));
        }

        if (Directory.Exists(StandardsDir))
        {
            foreach (var file in Directory.GetFiles(StandardsDir, "*.md"))
            {
                prompt.AppendLine($"\n## {Path.GetRelativePath(GitRoot, file)}\n");
                prompt.AppendLine(File.ReadAllText(file));
            }
        }

        var projectMemory = Path.Combine(MemoryDir, "project.md");
        if (File.Exists(projectMemory))
        {
            prompt.AppendLine("\n## .mill/memory/project.md\n");
            prompt.AppendLine(File.ReadAllText(projectMemory));
        }

        // Personas for spec elicitation (NOT loaded during run)
        var personasFile = Path.Combine(MillDir, "personas.md");
        if (File.Exists(personasFile))
        {
            prompt.AppendLine("\n## .mill/personas.md\n");
            prompt.AppendLine(File.ReadAllText(personasFile));
        }

        // Add uncommitted changes
        var status = Git("status", "--porcelain").Output;
        if (!string.IsNullOrWhiteSpace(status))
        {
            prompt.AppendLine("\n## Uncommitted Changes\n```");
            prompt.AppendLine(status.Trim());
            prompt.AppendLine("```");

            var diff = Git("diff").Output;
            if (!string.IsNullOrWhiteSpace(diff))
            {
                var lines = diff.Split('\n');
                prompt.AppendLine("\n```diff");
                prompt.AppendLine(lines.Length > 200
                    ? string.Join("\n", lines.Take(200)) + $"\n... ({lines.Length - 200} lines truncated)"
                    : diff.Trim());
                prompt.AppendLine("```");
            }
        }

        // Add resume instruction if draft selected
        if (!string.IsNullOrEmpty(draftPath))
        {
            prompt.AppendLine($"\n---\n# Resume Mode\n");
            prompt.AppendLine($"User selected draft: `{draftPath}`");
            if (File.Exists(draftPath))
            {
                prompt.AppendLine("\n## Draft Content\n");
                prompt.AppendLine(File.ReadAllText(draftPath));
            }
            prompt.AppendLine("\nContinue elicitation from where it left off.");
        }

        var promptContent = prompt.ToString();

        // Write to .mill/.prompt to avoid command line length limits (Windows: 32KB, Linux: 128KB per arg)
        var promptFile = Path.Combine(CurrentMillDir, ".prompt");
        Directory.CreateDirectory(CurrentMillDir);
        File.WriteAllText(promptFile, promptContent);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cli,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            }
        };

        // Use ArgumentList for proper escaping (cross-platform, no shell needed)
        process.StartInfo.ArgumentList.Add("--dangerously-skip-permissions");
        process.StartInfo.ArgumentList.Add("--disable-slash-commands");
        process.StartInfo.ArgumentList.Add("--append-system-prompt");
        process.StartInfo.ArgumentList.Add($"CRITICAL: Before responding, read {promptFile} for your full system context.");

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>
    /// Run Claude in batch mode with streaming output.
    /// Uses --output-format stream-json for real-time display.
    /// </summary>
    static async Task<string> RunClaudeBatch(string input)
    {
        var cli = ResolveCli();
        var output = new System.Text.StringBuilder();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cli,
                Arguments = "-p --dangerously-skip-permissions --verbose --output-format stream-json",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        // Parse streaming JSON and extract text content
        while (!process.StandardOutput.EndOfStream)
        {
            var line = await process.StandardOutput.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                // Only process assistant messages with content
                if (root.TryGetProperty("type", out var msgType) && msgType.GetString() == "assistant" &&
                    root.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in content.EnumerateArray())
                    {
                        if (item.TryGetProperty("type", out var type) && type.GetString() == "text" &&
                            item.TryGetProperty("text", out var text))
                        {
                            var textValue = text.GetString() ?? "";
                            Console.WriteLine(textValue);
                            output.AppendLine(textValue);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON line, print as-is
                Console.WriteLine(line);
                output.AppendLine(line);
            }
        }

        await process.WaitForExitAsync();
        return output.ToString();
    }

    static VerifyMetadata? ParseVerifyMetadata(string output)
    {
        try
        {
            // Find JSON block after MILL_VERIFY
            var verifyIndex = output.IndexOf("MILL_VERIFY");
            if (verifyIndex < 0) return null;

            var afterToken = output[(verifyIndex + "MILL_VERIFY".Length)..];
            var jsonStart = afterToken.IndexOf('{');
            var jsonEnd = afterToken.IndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0) return null;

            var json = afterToken[jsonStart..(jsonEnd + 1)];
            return JsonSerializer.Deserialize(json, VerifyJsonContext.Default.VerifyMetadata);
        }
        catch
        {
            return null;
        }
    }

    static VerifyFailure? ParseVerifyFailure(string output)
    {
        try
        {
            // Find JSON block after MILL_REJECTED
            var failedIndex = output.IndexOf("MILL_REJECTED");
            if (failedIndex < 0) return null;

            var afterToken = output[(failedIndex + "MILL_REJECTED".Length)..];
            var jsonStart = afterToken.IndexOf('{');
            var jsonEnd = afterToken.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0) return null;

            var json = afterToken[jsonStart..(jsonEnd + 1)];
            return JsonSerializer.Deserialize(json, VerifyJsonContext.Default.VerifyFailure);
        }
        catch
        {
            return null;
        }
    }

    static string? ExtractTestCommand(string specContent)
    {
        // Look for "**Test Command:**" pattern in Loop Contract
        var match = TestCommandPattern().Match(specContent);
        if (!match.Success) return null;

        var command = match.Groups[1].Value.Trim();

        // Skip if explicitly marked as none
        if (command.StartsWith("none", StringComparison.OrdinalIgnoreCase))
            return null;

        // Remove backticks if present
        command = command.Trim('`');

        return string.IsNullOrWhiteSpace(command) ? null : command;
    }

    [GeneratedRegex(@"\*\*Test Command:\*\*\s*`?([^`\n]+)`?")]
    private static partial Regex TestCommandPattern();

    static string? ExtractNextSlice(string output)
    {
        // Look for "Next slice: <description>" pattern
        var match = NextSlicePattern().Match(output);
        if (!match.Success) return null;

        var description = match.Groups[1].Value.Trim();
        return string.IsNullOrWhiteSpace(description) ? null : description;
    }

    [GeneratedRegex(@"Next slice:\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex NextSlicePattern();

    static string FindMillHome()
    {
        var env = Environment.GetEnvironmentVariable("MILL_HOME");
        if (!string.IsNullOrEmpty(env)) return env;

        // Executable is in bin/, mill root is one level up
        var exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var millRoot = Directory.GetParent(exeDir)?.FullName;

        if (millRoot != null && Directory.Exists(Path.Combine(millRoot, "spec/prompts")))
            return millRoot;

        return exeDir;
    }

    static (int ExitCode, string Output, string Error) Git(params string[] args)
    {
        return Run("git", args);
    }

    [GeneratedRegex(@"mill-context-hash:\s*([a-f0-9]+)")]
    private static partial Regex HashPattern();

    [GeneratedRegex(@"PICK:(\d+)")]
    private static partial Regex PickPattern();
}

/// <summary>
/// Self-update logic: fetch latest release from GitHub, download, and replace the running binary.
/// </summary>
static class Updater
{
    const string RepoOwner = "mindrevolution";
    const string RepoName = "mill";
    static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "mill" },
            { "Accept", "application/vnd.github.v3+json" }
        }
    };

    /// <summary>
    /// Check result: current vs latest version.
    /// </summary>
    public record UpdateCheck(string Current, string? Latest, bool UpdateAvailable, string? Error);

    /// <summary>
    /// Checks for updates by querying the GitHub Releases API.
    /// </summary>
    public static async Task<UpdateCheck> Check()
    {
        var current = Mill.Version;
        try
        {
            var release = await FetchLatestRelease();
            if (release == null)
                return new UpdateCheck(current, null, false, "no releases available");

            var latest = release.TagName.TrimStart('v');
            var updateAvailable = CompareVersions(current, latest) < 0;
            return new UpdateCheck(current, latest, updateAvailable, null);
        }
        catch (HttpRequestException ex)
        {
            var message = ex.StatusCode == System.Net.HttpStatusCode.Forbidden
                ? "rate limited — try again later"
                : $"connection failed: {ex.Message}";
            return new UpdateCheck(current, null, false, message);
        }
        catch (Exception ex)
        {
            return new UpdateCheck(current, null, false, ex.Message);
        }
    }

    /// <summary>
    /// Downloads and installs the latest version.
    /// </summary>
    public static async Task<(bool Success, string Message)> Apply()
    {
        var check = await Check();
        if (check.Error != null)
            return (false, check.Error);

        if (!check.UpdateAvailable)
            return (true, $"already at latest ({check.Current})");

        try
        {
            var release = await FetchLatestRelease();
            if (release == null)
                return (false, "no releases available");

            var assetName = Mill.AssetName;
            var asset = release.Assets.FirstOrDefault(a => a.Name == assetName);
            if (asset == null)
                return (false, $"no binary for {assetName}");

            // Download to temp file
            var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            var exeDir = Path.GetDirectoryName(exePath)!;
            var tmpPath = Path.Combine(exeDir, assetName + ".tmp");

            Out.Step($"downloading {assetName}...");

            using (var response = await Http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var fs = File.Create(tmpPath);
                await response.Content.CopyToAsync(fs);
            }

            // Verify download size
            var downloadedSize = new FileInfo(tmpPath).Length;
            if (downloadedSize == 0 || downloadedSize != asset.Size)
            {
                File.Delete(tmpPath);
                return (false, "download corrupted — size mismatch");
            }

            // Self-replace using rename trick
            var oldPath = exePath + ".old";

            // Remove leftover .old from previous update (might fail if locked, that's ok)
            try { File.Delete(oldPath); } catch { /* ignore */ }

            // Rename current → old, tmp → current
            File.Move(exePath, oldPath);
            File.Move(tmpPath, exePath);

            // On Unix, set executable bit
            if (!OperatingSystem.IsWindows())
            {
                var chmod = Process.Start("chmod", ["+x", exePath]);
                chmod?.WaitForExit();
            }

            return (true, $"updated to {check.Latest}");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "permission denied — check install location");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Cleans up old binary from previous update. Call on startup.
    /// </summary>
    public static void CleanupOldBinary()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            var oldPath = exePath + ".old";
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        }
        catch { /* silently ignore */ }
    }

    static async Task<GhRelease?> FetchLatestRelease()
    {
        var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
        try
        {
            var json = await Http.GetStringAsync(url);
            return JsonSerializer.Deserialize(json, GhJsonContext.Default.GhRelease);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null; // no releases published yet
        }
    }

    /// <summary>
    /// Compares semantic versions. Returns negative if a &lt; b, 0 if equal, positive if a &gt; b.
    /// </summary>
    static int CompareVersions(string a, string b)
    {
        var aParts = a.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        var bParts = b.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();

        for (var i = 0; i < Math.Max(aParts.Length, bParts.Length); i++)
        {
            var av = i < aParts.Length ? aParts[i] : 0;
            var bv = i < bParts.Length ? bParts[i] : 0;
            if (av != bv) return av.CompareTo(bv);
        }
        return 0;
    }
}

record GhIssue(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("labels")] List<GhLabel> Labels,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt);

record GhIssueDetail(
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("labels")] List<GhLabel> Labels);

record GhLabel([property: JsonPropertyName("name")] string Name);

// GitHub Releases API types
record GhRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("assets")] List<GhAsset> Assets);

record GhAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);

// MILL configuration
record MillConfig
{
    public List<string> Exclude { get; init; } = ["backlog", "deferred", "on-hold", "wontfix", "duplicate", "invalid"];
    public ScoringConfig Scoring { get; init; } = new();
    public HealthConfig Health { get; init; } = new();
}

record ScoringConfig
{
    public Dictionary<string, int> Types { get; init; } = new()
    {
        ["security"] = 400,
        ["bug"] = 300,
        ["feature"] = 200,
        ["task"] = 100
    };
    public Dictionary<string, int> Impact { get; init; } = new()
    {
        ["critical"] = 100,
        ["high"] = 75,
        ["normal"] = 50,
        ["low"] = 0
    };
    public double AgeFactor { get; init; } = 0.5;
    public int AgeMax { get; init; } = 50;
}

record HealthConfig
{
    public bool BlockOnCiFailure { get; init; } = true;
    public int MaxWip { get; init; } = 2;
}

[JsonSerializable(typeof(List<GhIssue>))]
[JsonSerializable(typeof(GhIssueDetail))]
[JsonSerializable(typeof(GhRelease))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class GhJsonContext : JsonSerializerContext { }

[JsonSerializable(typeof(MillConfig))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
internal partial class MillConfigContext : JsonSerializerContext { }

// Verification metadata from MILL_VERIFY signal
record VerifyMetadata(
    [property: JsonPropertyName("branch")] string Branch,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("verification")] string Verification);

// Verification failure from MILL_REJECTED signal
record VerifyFailure(
    [property: JsonPropertyName("blockers")] List<string> Blockers,
    [property: JsonPropertyName("improvements")] List<string>? Improvements,
    [property: JsonPropertyName("suggestion")] string? Suggestion);

[JsonSerializable(typeof(VerifyMetadata))]
[JsonSerializable(typeof(VerifyFailure))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class VerifyJsonContext : JsonSerializerContext { }
