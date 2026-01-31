using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;

Installer.CleanupOldBinary();
Out.Banner();

// API-only mode for development (no Photino window)
if (args is ["--api-only"] or ["-a"])
{
    return Workbench.RunApiOnly();
}

// Default: run workbench
// Windows: requires STA thread for WebView2/COM - see https://github.com/tryphotino/photino.NET/issues/180
// macOS/Linux: must run on main thread for AppKit/GTK
if (args is [])
{
    if (OperatingSystem.IsWindows())
    {
        var result = 0;
        var thread = new Thread(() => result = Workbench.Run());
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }
    return Workbench.Run();
}

return args switch
{
    ["--version"] or ["-v"] => Commands.ShowVersion(),
    ["install", "--check"] => await Commands.InstallCheck(),
    ["install"] => await Commands.Install(),
    ["init"] => await Mill.Init(),
    ["spec", var issue] when int.TryParse(issue, out _) => await Mill.RunSpecRefine(issue),
    ["spec", ..] => await Mill.RunSpec(),
    ["personas"] => Mill.RunPersonas(),
    ["standards"] => Mill.RunStandards(),
    ["sweep"] => await Mill.RunSweep(),
    ["run", "--auto"] => await Mill.RunAutopick(),
    ["run", "-a"] => await Mill.RunAutopick(),
    ["run", var issue, ..] => await Mill.Execute(issue, args),
    ["run"] => Mill.ListAvailableIssues(),
    _ => Commands.ShowHelp()
};

// ============================================================================
// Supporting classes below - to be refactored into separate files over time
// ============================================================================

/// <summary>
/// Standardized console output helpers. Minimal, uniform, hacker style.
/// </summary>
static class Out
{
    public static void Step(string msg)
    {
        WriteColored("  • ", ConsoleColor.DarkGray);
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

    /// <summary>
    /// Write LLM output with indentation and dimmed color.
    /// Handles multiline text by indenting each line.
    /// </summary>
    public static void Agent(string text)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        foreach (var line in text.Split('\n'))
        {
            Console.Write("    ");
            Console.WriteLine(line.TrimEnd('\r'));
        }
        Console.ForegroundColor = prev;
    }

    public static void Line() => Console.WriteLine("  ---");
    public static void Blank() => Console.WriteLine();

    public static void Prompt(string msg)
    {
        // Blinking ❯ symbol using ANSI escape codes
        Console.Write($"  \x1b[5m❯\x1b[0m {msg}");
    }

    public static bool Confirm(string msg)
    {
        Prompt($"{msg} [y/n] ");
        var key = Console.ReadKey(intercept: true);
        // Clear line, reprint without blink
        Console.Write($"\r\x1b[2K  ❯ {msg} [y/n] {key.KeyChar}");
        Console.WriteLine();
        return key.KeyChar is 'y' or 'Y';
    }

    public static void Banner()
    {
        // #ffcc00 = RGB(255, 204, 0), fallback to ConsoleColor.Yellow
        var trueColor = Environment.GetEnvironmentVariable("COLORTERM") is "truecolor" or "24bit";
        if (trueColor)
        {
            Console.WriteLine($"\x1b[38;2;255;204;0m■\x1b[0m mill {Mill.Version}");
        }
        else
        {
            WriteColored("■", ConsoleColor.Yellow);
            Console.WriteLine($" mill {Mill.Version}");
        }
        Blank();
    }

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
    static string ConfigFile => Path.Combine(ParentMillDir, "project.json");
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
        "task",
        // Sweep labels
        "sweep",
        "impact:low"
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
            if (!Out.Confirm("continue?"))
            {
                Out.Warn("aborted");
                Out.Blank();
                return 0;
            }

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
                    "sweep" => "BFD4F2",
                    "impact:low" => "C5DEF5",
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
        if (!gitignore.Contains("issue-*-plan.md"))
            gitignoreUpdates.Add(".mill/memory/issue-*-plan.md");

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

        // Auto-infer standards if not present
        var standardsFile = Path.Combine(StandardsDir, "code.md");
        if (!File.Exists(standardsFile))
        {
            Out.Blank();
            Out.Step("inferring standards...");
            Out.Blank();

            var standardsExitCode = await RunStandardsInference();
            if (standardsExitCode != 0)
            {
                Out.Warn("standards inference skipped");
            }
            else
            {
                Out.Ok("standards created");
            }
        }
        else
        {
            Out.Detail("standards already exist");
        }

        Out.Blank();
        Out.Line();
        Out.Blank();
        Out.Ok("ready — run: mill spec");
        Out.Blank();
        Out.Detail("tip: run `mill personas` to define user segments for better specs");
        Out.Detail("tip: run `mill standards` to refine inferred standards");
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

    /// <summary>
    /// Gets existing open sweep issues to avoid creating duplicates.
    /// Returns a set of finding IDs that already have open issues.
    /// </summary>
    static HashSet<string> GetExistingSweepIssues()
    {
        var (exit, output) = Gh("issue", "list", "--state", "open", "--label", "sweep", "--json", "body", "--limit", "100");
        if (exit != 0) return [];

        var issues = JsonSerializer.Deserialize(output, GhJsonContext.Default.ListGhIssueDetail) ?? [];
        var existingIds = new HashSet<string>();

        foreach (var issue in issues)
        {
            if (string.IsNullOrEmpty(issue.Body)) continue;

            // Extract finding ID from issue body (format: "Finding: `dry-001`" or similar)
            var match = Regex.Match(issue.Body, @"Finding:\s*`?([a-z]+-\d+)`?", RegexOptions.IgnoreCase);
            if (match.Success)
                existingIds.Add(match.Groups[1].Value.ToLowerInvariant());
        }

        return existingIds;
    }

    /// <summary>
    /// Reads dismissed findings from project memory.
    /// Returns a set of finding IDs that have been dismissed by the user.
    /// </summary>
    static HashSet<string> ReadDismissedFindings()
    {
        var projectMemory = Path.Combine(MemoryDir, "project.md");
        if (!File.Exists(projectMemory)) return [];

        var content = File.ReadAllText(projectMemory);
        var dismissed = new HashSet<string>();

        // Look for dismissed findings section
        var match = Regex.Match(content, @"## Dismissed Findings\s*\n((?:- .+\n?)+)", RegexOptions.IgnoreCase);
        if (!match.Success) return dismissed;

        // Extract each dismissed finding ID
        var section = match.Groups[1].Value;
        foreach (Match idMatch in Regex.Matches(section, @"- `?([a-z]+-\d+)`?", RegexOptions.IgnoreCase))
        {
            dismissed.Add(idMatch.Groups[1].Value.ToLowerInvariant());
        }

        return dismissed;
    }

    /// <summary>
    /// Appends dismissed findings to project memory.
    /// </summary>
    static void SaveDismissedFindings(IEnumerable<string> findingIds)
    {
        var projectMemory = Path.Combine(MemoryDir, "project.md");
        Directory.CreateDirectory(MemoryDir);

        var content = File.Exists(projectMemory) ? File.ReadAllText(projectMemory) : "";

        // Check if dismissed findings section exists
        if (content.Contains("## Dismissed Findings"))
        {
            // Append to existing section
            var lines = findingIds.Select(id => $"- `{id}`");
            var insertion = string.Join("\n", lines) + "\n";

            // Find the end of the section and insert
            var sectionEnd = content.IndexOf("\n## ", content.IndexOf("## Dismissed Findings") + 1);
            if (sectionEnd == -1)
            {
                // Section is at the end of file
                content = content.TrimEnd() + "\n" + insertion;
            }
            else
            {
                content = content.Insert(sectionEnd, insertion);
            }
        }
        else
        {
            // Create new section
            var section = "\n## Dismissed Findings\n\n" + string.Join("\n", findingIds.Select(id => $"- `{id}`")) + "\n";
            content = content.TrimEnd() + section;
        }

        File.WriteAllText(projectMemory, content);
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

        // Initial message based on mode
        var initialMessage = selectedDraft != null
            ? "continue this draft"
            : "start";

        return RunClaudeInteractive("spec/prompts/spec-draft.md", selectedDraft, initialMessage);
    }

    public static async Task<int> RunSpecRefine(string issueNumber)
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

        // Fetch issue
        Out.Step($"fetching issue #{issueNumber}...");
        var (exitCode, output) = Gh("issue", "view", issueNumber, "--json", "body,title,state,labels");
        if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            Out.Error($"failed to fetch issue #{issueNumber}");
            return 1;
        }

        var issueDetail = JsonSerializer.Deserialize(output, GhJsonContext.Default.GhIssueDetailFull);
        if (issueDetail == null)
        {
            Out.Error("failed to parse issue");
            return 1;
        }

        if (issueDetail.State.Equals("CLOSED", StringComparison.OrdinalIgnoreCase))
        {
            Out.Warn("issue is closed");
            return 0;
        }

        Out.Ok($"loaded: {issueDetail.Title}");

        // Print clickable URL
        var (repoExit, repoUrl) = Gh("repo", "view", "--json", "url", "-q", ".url");
        if (repoExit == 0)
            Out.Detail($"{repoUrl.Trim()}/issues/{issueNumber}");

        // Check context staleness
        var (needsWarmup, reason) = CheckContextStaleness();
        if (needsWarmup)
        {
            Out.Warn(reason.ToLower());
            Out.Blank();
            Out.Step("building context...");
            Out.Blank();

            var warmupExitCode = await RunClaudeStreaming("spec/prompts/context-warmup.md");
            if (warmupExitCode != 0) return warmupExitCode;

            Out.Blank();
            Out.Line();
            Out.Blank();
        }

        Out.Ok($"context loaded ({GetContextInfo()})");
        Out.Blank();

        return RunClaudeInteractiveRefine("spec/prompts/spec-refine.md", issueNumber, issueDetail.Body ?? "");
    }

    static int RunClaudeInteractiveRefine(string promptPath, string issueNumber, string issueBody)
    {
        var fullPath = Path.Combine(FindMillHome(), promptPath);
        if (!File.Exists(fullPath))
        {
            Out.Error($"prompt not found: {fullPath}");
            return 1;
        }

        var cli = RequireCli();
        if (cli == null) return 1;

        // Build prompt with pre-loaded context
        var prompt = new System.Text.StringBuilder();
        prompt.AppendLine(File.ReadAllText(fullPath).Replace("{{ISSUE_NUMBER}}", issueNumber));

        prompt.AppendLine("\n---\n# Pre-loaded Context\n");

        // The existing spec
        prompt.AppendLine("## Current Spec (GitHub Issue)\n");
        prompt.AppendLine(issueBody);

        // Context file
        if (File.Exists(ContextFile))
        {
            prompt.AppendLine("\n## .mill/context.md\n");
            prompt.AppendLine(File.ReadAllText(ContextFile));
        }

        // Standards
        if (Directory.Exists(StandardsDir))
        {
            foreach (var file in Directory.GetFiles(StandardsDir, "*.md"))
            {
                prompt.AppendLine($"\n## {Path.GetRelativePath(GitRoot, file)}\n");
                prompt.AppendLine(File.ReadAllText(file));
            }
        }

        // Project memory
        var projectMemory = Path.Combine(MemoryDir, "project.md");
        if (File.Exists(projectMemory))
        {
            prompt.AppendLine("\n## .mill/memory/project.md\n");
            prompt.AppendLine(File.ReadAllText(projectMemory));
        }

        // Uncommitted changes
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

        var promptContent = prompt.ToString();

        // Write to .mill/.prompt
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
        // Initial message to kick off analysis
        process.StartInfo.ArgumentList.Add("analyze this spec against the current codebase");

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
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
        var hasExisting = File.Exists(personasFile);

        if (hasExisting)
        {
            Out.Ok("existing personas found");
            Out.Detail("you can update, add, or remove personas");
        }
        else
        {
            Out.Step("no personas yet — starting creation");
        }

        Out.Blank();

        return RunClaudeInteractivePersonas("spec/prompts/personas.md", hasExisting);
    }

    static int RunClaudeInteractivePersonas(string promptPath, bool hasExistingPersonas)
    {
        var fullPath = Path.Combine(FindMillHome(), promptPath);
        if (!File.Exists(fullPath))
        {
            Out.Error($"prompt not found: {fullPath}");
            return 1;
        }

        var cli = RequireCli();
        if (cli == null) return 1;

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

        // Initial message based on mode
        var initialMessage = hasExistingPersonas
            ? "review and update these personas"
            : "help me define user personas for this project";
        process.StartInfo.ArgumentList.Add(initialMessage);

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    public static int RunStandards()
    {
        if (!IsGitRepo())
        {
            Out.Error("not in a git repository");
            return 1;
        }

        Out.Blank();

        var standardsFile = Path.Combine(StandardsDir, "code.md");
        var hasExisting = File.Exists(standardsFile);

        if (hasExisting)
        {
            Out.Ok("existing standards found");
            Out.Detail("you can update, add, or regenerate");
        }
        else
        {
            Out.Step("no standards yet — starting inference");
        }

        Out.Blank();

        return RunClaudeInteractiveStandards("spec/prompts/standards-infer.md", hasExisting);
    }

    static int RunClaudeInteractiveStandards(string promptPath, bool hasExistingStandards)
    {
        var fullPath = Path.Combine(FindMillHome(), promptPath);
        if (!File.Exists(fullPath))
        {
            Out.Error($"prompt not found: {fullPath}");
            return 1;
        }

        var cli = RequireCli();
        if (cli == null) return 1;

        // Build prompt with pre-loaded context for standards inference
        var prompt = new System.Text.StringBuilder();
        prompt.AppendLine(File.ReadAllText(fullPath));

        prompt.AppendLine("\n---\n# Pre-loaded Context\n");

        // context.md if available
        if (File.Exists(ContextFile))
        {
            prompt.AppendLine("## .mill/context.md\n");
            prompt.AppendLine(File.ReadAllText(ContextFile));
        }

        // AGENTS.md for project context
        var agentsPath = Path.Combine(GitRoot, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            prompt.AppendLine("\n## AGENTS.md\n");
            prompt.AppendLine(File.ReadAllText(agentsPath));
        }

        // Existing standards for update mode
        var standardsFile = Path.Combine(StandardsDir, "code.md");
        if (File.Exists(standardsFile))
        {
            prompt.AppendLine("\n## Existing Standards (.mill/standards/code.md)\n");
            prompt.AppendLine(File.ReadAllText(standardsFile));
            prompt.AppendLine("\n**Mode:** Update existing standards based on new analysis or user input.");
        }

        // List config files that might contain standards
        prompt.AppendLine("\n## Config Files Detected\n");
        var configFiles = DetectConfigFiles();
        if (configFiles.Count > 0)
        {
            foreach (var (name, content) in configFiles)
            {
                prompt.AppendLine($"### {name}\n```");
                prompt.AppendLine(content.Length > 2000 ? content[..2000] + "\n... (truncated)" : content);
                prompt.AppendLine("```\n");
            }
        }
        else
        {
            prompt.AppendLine("No standard config files detected (.editorconfig, .eslintrc, etc.)");
        }

        var promptContent = prompt.ToString();

        // Write to .mill/.prompt to avoid command line length limits
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

        // Initial message based on mode
        var initialMessage = hasExistingStandards
            ? "review and update these standards"
            : "infer standards from this codebase";
        process.StartInfo.ArgumentList.Add(initialMessage);

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    static List<(string Name, string Content)> DetectConfigFiles()
    {
        var configFiles = new List<(string Name, string Content)>();
        var configPatterns = new[]
        {
            ".editorconfig",
            ".eslintrc",
            ".eslintrc.js",
            ".eslintrc.json",
            ".eslintrc.yml",
            "eslint.config.js",
            "eslint.config.mjs",
            ".prettierrc",
            ".prettierrc.js",
            ".prettierrc.json",
            "prettier.config.js",
            "stylecop.json",
            ".stylecop",
            "tsconfig.json",
            "biome.json",
            ".rubocop.yml",
            "pyproject.toml",
            "setup.cfg",
            ".flake8",
            "go.mod"
        };

        foreach (var pattern in configPatterns)
        {
            var path = Path.Combine(GitRoot, pattern);
            if (File.Exists(path))
            {
                try
                {
                    configFiles.Add((pattern, File.ReadAllText(path)));
                }
                catch { }
            }
        }

        return configFiles;
    }

    /// <summary>
    /// Run standards inference in non-interactive streaming mode (for mill init).
    /// Creates .mill/standards/code.md with inferred rules.
    /// </summary>
    static async Task<int> RunStandardsInference()
    {
        var promptPath = Path.Combine(FindMillHome(), "spec/prompts/standards-infer.md");
        if (!File.Exists(promptPath))
        {
            Out.Error($"standards prompt not found: {promptPath}");
            return 1;
        }

        // Build the prompt with context
        var prompt = new System.Text.StringBuilder();
        prompt.AppendLine(File.ReadAllText(promptPath));
        prompt.AppendLine("\n---\n# Pre-loaded Context\n");

        // context.md if available
        if (File.Exists(ContextFile))
        {
            prompt.AppendLine("## .mill/context.md\n");
            prompt.AppendLine(File.ReadAllText(ContextFile));
        }

        // AGENTS.md for project context
        var agentsPath = Path.Combine(GitRoot, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            prompt.AppendLine("\n## AGENTS.md\n");
            prompt.AppendLine(File.ReadAllText(agentsPath));
        }

        // List config files
        prompt.AppendLine("\n## Config Files Detected\n");
        var configFiles = DetectConfigFiles();
        if (configFiles.Count > 0)
        {
            foreach (var (name, content) in configFiles)
            {
                prompt.AppendLine($"### {name}\n```");
                prompt.AppendLine(content.Length > 2000 ? content[..2000] + "\n... (truncated)" : content);
                prompt.AppendLine("```\n");
            }
        }
        else
        {
            prompt.AppendLine("No standard config files detected (.editorconfig, .eslintrc, etc.)");
        }

        // Add auto-inference instruction
        prompt.AppendLine("\n---\n# Auto-Inference Mode\n");
        prompt.AppendLine("This is running during `mill init`. Generate standards non-interactively:");
        prompt.AppendLine("1. Detect config files and analyze codebase patterns");
        prompt.AppendLine("2. Generate `.mill/standards/code.md` with inferred rules");
        prompt.AppendLine("3. If no clear patterns found, create minimal standards with placeholders");
        prompt.AppendLine("4. Do NOT ask questions — just infer and write");

        var cli = RequireCli();
        if (cli == null) return 1;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cli,
                Arguments = "-p --dangerously-skip-permissions",
                RedirectStandardInput = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.StandardInput.WriteAsync(prompt.ToString());
        process.StandardInput.Close();
        await process.WaitForExitAsync();

        return process.ExitCode;
    }

    /// <summary>
    /// Scan codebase against standards, surface findings, and optionally create GitHub issues.
    /// </summary>
    public static async Task<int> RunSweep()
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

        // Check for standards
        var standardsFile = Path.Combine(StandardsDir, "code.md");
        if (!File.Exists(standardsFile))
        {
            Out.Blank();
            Out.Warn("no standards defined");
            Out.Detail("sweep requires standards to check against");
            Out.Blank();
            Console.WriteLine("  run: mill init      (infers standards during setup)");
            Console.WriteLine("  or:  mill standards (create/edit standards manually)");
            Out.Blank();
            return 1;
        }

        Out.Blank();
        Out.Step("analyzing codebase against standards...");
        Out.Blank();

        // Run Claude to analyze the codebase
        var sweepPromptPath = Path.Combine(FindMillHome(), "run/prompts/sweep-analyze.md");
        if (!File.Exists(sweepPromptPath))
        {
            Out.Error($"sweep prompt not found: {sweepPromptPath}");
            return 1;
        }

        // Build the prompt with standards content
        var template = await File.ReadAllTextAsync(sweepPromptPath);
        var standardsContent = await File.ReadAllTextAsync(standardsFile);
        var rendered = template.Replace("{{STANDARDS_CONTENT}}", standardsContent);

        // Run Claude and capture output
        var cli = RequireCli();
        if (cli == null) return 1;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cli,
                Arguments = "-p --dangerously-skip-permissions --output-format text",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = false, // Let errors go to console
                UseShellExecute = false
            }
        };

        process.Start();
        await process.StandardInput.WriteAsync(rendered);
        process.StandardInput.Close();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Out.Error("analysis failed");
            return 1;
        }

        // Parse the JSON output
        var analysisMatch = Regex.Match(output, @"SWEEP_ANALYSIS\s*(\{[\s\S]*\})", RegexOptions.Multiline);
        if (!analysisMatch.Success)
        {
            Out.Warn("could not parse analysis output");
            Out.Detail("check the sweep-analyze prompt format");
            return 1;
        }

        SweepAnalysis? analysis;
        try
        {
            analysis = JsonSerializer.Deserialize(analysisMatch.Groups[1].Value, GhJsonContext.Default.SweepAnalysis);
        }
        catch (JsonException ex)
        {
            Out.Error($"invalid analysis JSON: {ex.Message}");
            return 1;
        }

        if (analysis == null || analysis.Categories.Count == 0)
        {
            Out.Blank();
            Out.Ok("no findings — codebase looks good!");
            Out.Blank();
            return 0;
        }

        // Load existing sweep issues and dismissed findings to filter
        var existingIssues = GetExistingSweepIssues();
        var dismissed = ReadDismissedFindings();

        // Filter findings per category
        var filteredCategories = new List<SweepCategory>();
        foreach (var category in analysis.Categories)
        {
            var filtered = category.Findings
                .Where(f => !existingIssues.Contains(f.Id.ToLowerInvariant()))
                .Where(f => !dismissed.Contains(f.Id.ToLowerInvariant()))
                .ToList();

            if (filtered.Count > 0)
            {
                filteredCategories.Add(category with { Findings = filtered });
            }
        }

        if (filteredCategories.Count == 0)
        {
            Out.Blank();
            Out.Ok("no new findings (existing issues or dismissed)");
            Out.Blank();
            return 0;
        }

        // Interactive selection per category
        var selectedFindings = new List<SweepFinding>();
        var toDismiss = new List<string>();

        Out.Blank();
        Console.WriteLine($"  found {filteredCategories.Sum(c => c.Findings.Count)} findings in {filteredCategories.Count} categories");
        Out.Blank();

        foreach (var category in filteredCategories)
        {
            Out.Line();
            Out.Blank();
            Console.WriteLine($"  {category.Name}");
            if (!string.IsNullOrEmpty(category.Description))
                Out.Detail(category.Description);
            Out.Blank();

            // Display numbered findings
            for (var i = 0; i < category.Findings.Count; i++)
            {
                var f = category.Findings[i];
                var severity = f.Severity?.ToLowerInvariant() switch
                {
                    "high" => "[high]",
                    "medium" => "[med]",
                    _ => "[low]"
                };
                Console.WriteLine($"    {i + 1}. {severity,-6} {f.Title}");
                if (!string.IsNullOrEmpty(f.File))
                    Out.Detail($"{f.File}" + (f.Line > 0 ? $":{f.Line}" : ""));
            }

            Out.Blank();
            Out.Prompt($"create issues [1-{category.Findings.Count}, all, none] (enter to skip): ");

            var input = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
            // Clear line and reprint without blink
            Console.Write($"\r\x1b[2K  ❯ selection: {(string.IsNullOrEmpty(input) ? "skip" : input)}");
            Console.WriteLine();

            if (string.IsNullOrEmpty(input))
            {
                // Default: skip
                continue;
            }
            else if (input == "all")
            {
                selectedFindings.AddRange(category.Findings);
            }
            else if (input == "none")
            {
                // Prompt to remember as dismissed
                if (Out.Confirm("remember as dismissed?"))
                {
                    toDismiss.AddRange(category.Findings.Select(f => f.Id));
                }
            }
            else
            {
                // Parse comma-separated numbers
                var indices = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var n) ? n : -1)
                    .Where(n => n >= 1 && n <= category.Findings.Count)
                    .Distinct()
                    .ToList();

                foreach (var idx in indices)
                {
                    selectedFindings.Add(category.Findings[idx - 1]);
                }
            }
        }

        // Save dismissed findings
        if (toDismiss.Count > 0)
        {
            SaveDismissedFindings(toDismiss);
            Out.Blank();
            Out.Ok($"dismissed {toDismiss.Count} findings");
        }

        // Create issues for selected findings
        if (selectedFindings.Count == 0)
        {
            Out.Blank();
            Out.Ok("no issues created");
            Out.Blank();
            return 0;
        }

        Out.Blank();
        Out.Step($"creating {selectedFindings.Count} issues...");
        Out.Blank();

        var created = 0;
        foreach (var finding in selectedFindings)
        {
            var title = $"[sweep] {finding.Title}";
            var body = $"""
                **Finding:** `{finding.Id}`

                {finding.Description}

                **Location:** {finding.File ?? "N/A"}{(finding.Line > 0 ? $":{finding.Line}" : "")}
                **Severity:** {finding.Severity ?? "low"}

                ---
                *Generated by `mill sweep`*
                """;

            var (exitCode, _) = Gh("issue", "create",
                "--title", title,
                "--body", body,
                "--label", "task",
                "--label", "impact:low",
                "--label", "sweep");

            if (exitCode == 0)
            {
                created++;
                Out.Detail($"created: {finding.Title}");
            }
            else
            {
                Out.Warn($"failed: {finding.Title}");
            }
        }

        Out.Blank();
        Out.Ok($"{created} issues created");
        Out.Detail("run: mill run to see available issues");
        Out.Blank();

        return 0;
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
        Out.Prompt($"[1-{drafts.Count}] or enter for new: ");

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

    static async Task<SpecLoadResult> LoadSpec(string issue)
    {
        var issueNumber = issue;
        var isGhIssue = int.TryParse(issueNumber, out _);

        if (isGhIssue)
        {
            Out.Step($"fetching issue #{issueNumber}...");
            var (exitCode, output) = Gh("issue", "view", issueNumber, "--json", "body,state,labels");
            if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                Out.Error($"failed to fetch issue #{issueNumber}");
                return new SpecLoadResult(null, 1);
            }

            var issueDetail = JsonSerializer.Deserialize(output, GhJsonContext.Default.GhIssueDetail);
            if (issueDetail == null)
            {
                Out.Error("failed to parse issue");
                return new SpecLoadResult(null, 1);
            }

            // Guard: check issue state and labels
            if (issueDetail.State.Equals("CLOSED", StringComparison.OrdinalIgnoreCase))
            {
                Out.Warn("issue is closed — work was merged");
                return new SpecLoadResult(null, 0);
            }

            var labels = issueDetail.Labels.Select(l => l.Name.ToLowerInvariant()).ToList();
            if (labels.Contains("ready-for-review"))
            {
                Out.Warn("issue has 'ready-for-review' — remove label to re-run");
                return new SpecLoadResult(null, 0);
            }
            if (labels.Contains("in-progress"))
            {
                Out.Warn("issue has 'in-progress' — already running?");
                return new SpecLoadResult(null, 0);
            }
            if (labels.Contains("blocked"))
            {
                Out.Warn("issue has 'blocked' — resolve before running");
                return new SpecLoadResult(null, 0);
            }

            var specContent = issueDetail.Body?.Trim() ?? "";
            var specRef = $"#{issueNumber}";
            Out.Ok($"loaded issue #{issueNumber}");

            // Add in-progress label
            Out.Step("marking in-progress...");
            Gh("issue", "edit", issueNumber, "--remove-label", "ready-for-review,blocked", "--add-label", "in-progress");

            return new SpecLoadResult(new SpecInfo(specContent, specRef, issueNumber, true), null);
        }
        else
        {
            // Fallback: local file (for offline/gh-unavailable scenarios)
            if (!File.Exists(issue))
            {
                Out.Error($"spec not found: {issue}");
                return new SpecLoadResult(null, 1);
            }
            var specContent = await File.ReadAllTextAsync(issue);
            var specRef = issue;
            return new SpecLoadResult(new SpecInfo(specContent, specRef, issueNumber, false), null);
        }
    }

    static WorktreeSetupResult SetupWorktree(string issueNumber, bool isGhIssue, string issue)
    {
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
            return new WorktreeSetupResult(null, 1);
        }
        Out.Ok($"worktree created (branch: {branchName})");

        return new WorktreeSetupResult(new WorktreeInfo(worktreePath, branchName, originalDir), null);
    }

    static async Task<VerificationResult> RunTestsAndVerify(string specContent, string specRef)
    {
        var testCommand = ExtractTestCommand(specContent);
        var criteria = ExtractCriteria(specContent);

        Out.Blank();

        // Step 1: Run tests once (gate before criterion verification)
        if (!string.IsNullOrEmpty(testCommand))
        {
            Out.Step($"running tests: {testCommand}");
            var (shellCmd, shellArg) = OperatingSystem.IsWindows()
                ? (FindInPath("pwsh") != null ? ("pwsh", "-c") : ("powershell.exe", "-Command"))
                : ("sh", "-c");
            var (testExit, testOut, testErr) = Run(shellCmd, shellArg, testCommand);
            if (testExit != 0)
            {
                Out.Blank();
                Out.Warn("tests failed");
                Out.Detail(testErr.Length > 0 ? testErr.Trim() : testOut.Trim());
                var failureContext = $"Tests failed: {(testErr.Length > 0 ? testErr.Trim() : testOut.Trim())}";
                return new VerificationResult(false, failureContext);
            }
            Out.Ok("tests passed");
        }

        // Step 2: Verify criteria
        if (criteria.Count > 0)
        {
            Out.Step($"verifying {criteria.Count} acceptance criteria...");
            Out.Blank();

            var (allPassed, results, failCtx) = await RunCriterionVerification(specContent, specRef, criteria);

            Out.Blank();
            if (allPassed)
            {
                Out.Ok($"all {results.Count} criteria passed");
            }
            else
            {
                var failures = results.Where(r => !r.Passed).ToList();
                Out.Warn($"{failures.Count}/{results.Count} criteria failed:");
                foreach (var f in failures)
                {
                    Out.Detail($"[{f.Index}] {f.Title}: {f.Reason}");
                }
                return new VerificationResult(false, failCtx);
            }
        }
        else
        {
            // No parseable criteria — skip verification with warning
            Out.Blank();
            Out.Warn("no acceptance criteria found in spec");
            Out.Detail("specs should have structured criteria for proper verification");
            Out.Detail("continuing without criterion verification...");
        }

        return new VerificationResult(true, null);
    }

    static PullRequestResult CreatePullRequest(VerifyMetadata metadata, string issueNumber)
    {
        // Push branch
        Out.Step($"pushing branch {metadata.Branch}...");
        var (pushExit, _, pushErr) = Run("git", "push", "-u", "origin", metadata.Branch);
        if (pushExit != 0)
        {
            Out.Warn($"push failed: {pushErr.Trim()}");
            return new PullRequestResult(false, $"Push failed: {pushErr.Trim()}");
        }

        // Create PR
        Out.Step("creating PR...");
        var prBody = $"Fixes #{issueNumber}\n\n## Summary\n{metadata.Summary}\n\n## Verification\n{metadata.Verification}";
        var (prExit, prOut, prErr) = Run("gh", "pr", "create", "--title", metadata.Title, "--body", prBody);
        if (prExit != 0)
        {
            Out.Warn($"PR creation failed: {prErr.Trim()}");
            return new PullRequestResult(false, $"PR creation failed: {prErr.Trim()}");
        }
        Out.Detail(prOut.Trim());

        // Mark ready-for-review
        Out.Step("marking ready-for-review...");
        Run("gh", "issue", "edit", issueNumber, "--remove-label", "in-progress", "--add-label", "ready-for-review");

        return new PullRequestResult(true, null);
    }

    public static async Task<int> Execute(string issue, string[] args)
    {
        if (!IsInitialized())
        {
            Out.Warn("mill not initialized — run: mill init");
            return 1;
        }

        // Check if local branch is behind remote
        Out.Step("syncing with remote...");
        Git("fetch");
        var (_, upstream, _) = Git("rev-parse", "--abbrev-ref", "@{upstream}");
        upstream = upstream.Trim();
        if (!string.IsNullOrEmpty(upstream))
        {
            var (_, behindStr, _) = Git("rev-list", "--count", $"HEAD..{upstream}");
            if (int.TryParse(behindStr.Trim(), out var behind) && behind > 0)
            {
                Out.Warn($"local branch is {behind} commit{(behind == 1 ? "" : "s")} behind {upstream}");
                if (!Out.Confirm("continue anyway?"))
                {
                    Out.Warn("aborted — run: git pull");
                    return 0;
                }
            }
        }

        var maxIterations = int.TryParse(Environment.GetEnvironmentVariable("MILL_MAX_ITERATIONS"), out var n) ? n : 20;
        const string verifyToken = "MILL_VERIFY";
        const string doneToken = "MILL_DONE";

        // Load spec from GitHub issue or local file
        var specResult = await LoadSpec(issue);
        if (specResult.ExitCode.HasValue)
            return specResult.ExitCode.Value;

        var (specContent, specRef, issueNumber, isGhIssue) = specResult.Spec!;

        // Validate prompts exist before starting
        var iterationPrompt = Path.Combine(FindMillHome(), "run/prompts/loop-iterate.md");
        var criterionPrompt = Path.Combine(FindMillHome(), "run/prompts/loop-verify-criterion.md");
        if (!File.Exists(iterationPrompt))
        {
            Out.Error($"iteration prompt not found: {iterationPrompt}");
            Out.Detail("check MILL_HOME environment variable or installation");
            return 1;
        }
        if (!File.Exists(criterionPrompt))
        {
            Out.Error($"criterion prompt not found: {criterionPrompt}");
            Out.Detail("check MILL_HOME environment variable or installation");
            return 1;
        }

        // Create isolated worktree for execution
        var worktreeResult = SetupWorktree(issueNumber, isGhIssue, issue);
        if (worktreeResult.ExitCode.HasValue)
            return worktreeResult.ExitCode.Value;

        var (worktreePath, branchName, originalDir) = worktreeResult.Worktree!;

        var result = 1;
        try
        {
            // Change to worktree directory
            Directory.SetCurrentDirectory(Path.Combine(originalDir, worktreePath));

            // Try to inherit parent context or determine warmup reason
            string? warmupReason = null;
            var parentContextFile = Path.Combine(ParentMillDir, "context.md");
            if (File.Exists(parentContextFile))
            {
                var firstLine = File.ReadLines(parentContextFile).FirstOrDefault() ?? "";
                var match = HashPattern().Match(firstLine);
                if (match.Success)
                {
                    var parentHash = match.Groups[1].Value;
                    var worktreeHead = Git("rev-parse", "HEAD").Output.Trim();

                    // Check commit distance from parent context
                    var (distance, threshold) = GetContextDistance(parentHash);
                    if (distance == null)
                    {
                        warmupReason = $"parent context hash {parentHash[..7]} not in history";
                    }
                    else if (distance.Value <= threshold)
                    {
                        // Within threshold - inherit parent context
                        Directory.CreateDirectory(CurrentMillDir);
                        File.Copy(parentContextFile, ContextFile, overwrite: true);
                        if (distance.Value == 0)
                            Out.Ok("context inherited from parent");
                        else
                            Out.Ok($"context inherited from parent ({distance} commits behind)");
                    }
                    else
                    {
                        warmupReason = $"parent context stale ({distance} commits behind, threshold {threshold})";
                    }
                }
            }

            // Fall back to staleness check if parent didn't resolve the situation
            if (warmupReason == null)
            {
                var (needsWarmup, reason) = CheckContextStaleness();
                if (needsWarmup)
                    warmupReason = reason.ToLower();
            }

            if (warmupReason != null)
            {
                Out.Warn(warmupReason);
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

                if (output.Contains("MILL_ABORT"))
                {
                    // Spec is invalid or impossible - abort cleanly
                    var abortReason = ExtractAbortReason(output);
                    Out.Blank();
                    Out.Warn("spec aborted");
                    if (abortReason != null)
                        Out.Detail(abortReason);

                    if (isGhIssue)
                    {
                        // Add comment explaining why and close the issue
                        var comment = $"🤖 **MILL_ABORT**: {abortReason ?? "Spec is invalid or impossible to implement."}";
                        Gh("issue", "comment", issueNumber, "--body", comment);
                        Gh("issue", "close", issueNumber, "--reason", "not planned");
                        Out.Ok($"issue #{issueNumber} closed");
                    }

                    result = 0; // Clean exit - abort is not an error
                    break;
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

                    // Run tests and verify criteria
                    var verifyResult = await RunTestsAndVerify(specContent, specRef);

                    if (verifyResult.Passed)
                    {
                        Out.Blank();
                        Out.Ok("verification passed");

                        if (isGhIssue)
                        {
                            var prResult = CreatePullRequest(metadata, issueNumber);
                            if (!prResult.Success)
                            {
                                lastFailureContext = prResult.Error;
                                continue;
                            }
                        }

                        Out.Blank();
                        Out.Ok(doneToken);
                        result = 0;
                        break;
                    }
                    else
                    {
                        lastFailureContext = verifyResult.FailureContext;
                        continue;
                    }
                }
                else
                {
                    // No recognized signal - warn and continue
                    Out.Blank();
                    Out.Warn("no signal (expected MILL_CONTINUE, MILL_VERIFY, or MILL_ABORT)");
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

        var baseUrl = repoUrl.Trim();
        var shown = sorted.Take(10).ToList();
        foreach (var issue in shown)
        {
            var impact = GetImpactLabel(issue.Labels);
            var age = FormatAge(issue.CreatedAt);
            var impactCol = impact != null ? $"[{impact}]".PadRight(7) : "       ";
            var title = issue.Title.Length > 40 ? issue.Title[..37] + "..." : issue.Title;
            var url = $"{baseUrl}/issues/{issue.Number}";
            Console.WriteLine($"  #{issue.Number,-4} {impactCol} {title,-40} {age,-8} {url}");
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

        // Exact match - no rebuild needed
        if (contextHash == currentHash)
            return (false, "");

        // Check commit distance
        var (distanceResult, threshold) = GetContextDistance(contextHash);
        if (distanceResult == null)
            return (true, $"Context hash {contextHash[..7]} not in history");

        return distanceResult.Value > threshold
            ? (true, $"Context stale ({distanceResult} commits behind)")
            : (false, "");
    }

    /// <summary>
    /// Get number of commits between context hash and HEAD, plus configured threshold.
    /// Returns null if contextHash is not an ancestor of HEAD (rebased away).
    /// </summary>
    static (int? Distance, int Threshold) GetContextDistance(string contextHash)
    {
        var config = LoadConfig();
        var threshold = config.Context.StaleThreshold;

        // Check if contextHash exists and is ancestor of HEAD
        var result = Git("rev-list", "--count", $"{contextHash}..HEAD");
        if (result.ExitCode != 0)
            return (null, threshold);

        if (int.TryParse(result.Output.Trim(), out var distance))
            return (distance, threshold);

        return (null, threshold);
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
    /// Resolve Claude CLI executable path.
    /// Priority: CLAUDE_PATH env var → PATH lookup → native install location
    /// </summary>
    static string? ResolveCli()
    {
        // 1. Explicit override via env var
        var explicitPath = Environment.GetEnvironmentVariable("CLAUDE_PATH");
        if (!string.IsNullOrEmpty(explicitPath))
        {
            if (File.Exists(explicitPath))
                return explicitPath;
            Out.Warn($"CLAUDE_PATH set but file not found: {explicitPath}");
        }

        // 2. Find in PATH
        var found = FindInPath("claude");
        if (found != null)
            return found;

        // 3. Native install location fallback
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var nativePath = OperatingSystem.IsWindows()
            ? Path.Combine(userProfile, ".local", "bin", "claude.exe")
            : Path.Combine(userProfile, ".local", "bin", "claude");

        if (File.Exists(nativePath))
            return nativePath;

        return null;
    }

    /// <summary>
    /// Resolve CLI or show error with install instructions.
    /// </summary>
    static string? RequireCli()
    {
        var cli = ResolveCli();
        if (cli != null)
            return cli;

        Out.Error("claude CLI not found");
        Out.Detail("install: https://code.claude.com/docs/en/setup");
        Out.Detail("or set CLAUDE_PATH env var to explicit path");
        return null;
    }

    /// <summary>
    /// Find executable in PATH using platform-specific lookup.
    /// </summary>
    static string? FindInPath(string name)
    {
        try
        {
            var (cmd, args) = OperatingSystem.IsWindows()
                ? ("where.exe", name)
                : ("which", name);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadLine(); // First match
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output) && File.Exists(output))
                return output;
        }
        catch
        {
            // where.exe or which not available, fall through
        }

        return null;
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
        var cli = RequireCli();
        if (cli == null) return 1;

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
    static int RunClaudeInteractive(string promptPath, string? draftPath = null, string? initialMessage = null)
    {
        var fullPath = Path.Combine(FindMillHome(), promptPath);
        if (!File.Exists(fullPath))
        {
            Out.Error($"prompt not found: {fullPath}");
            return 1;
        }

        var cli = RequireCli();
        if (cli == null) return 1;

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

        // Initial message to kick off the workflow
        if (!string.IsNullOrEmpty(initialMessage))
            process.StartInfo.ArgumentList.Add(initialMessage);

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
        var cli = RequireCli();
        if (cli == null) return "";

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
        string? line;
        while ((line = await process.StandardOutput.ReadLineAsync()) is not null)
        {
            if (line.Length == 0) continue;

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
                            Out.Agent(textValue);
                            output.AppendLine(textValue);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON line, print as-is
                Out.Agent(line);
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

    /// <summary>
    /// Extract acceptance criteria from spec for parallel verification.
    /// </summary>
    static List<AcceptanceCriterion> ExtractCriteria(string specContent)
    {
        var criteria = new List<AcceptanceCriterion>();

        // Pattern A: Numbered items under "## Acceptance Criteria"
        var acMatch = AcceptanceCriteriaSection().Match(specContent);
        if (acMatch.Success)
        {
            var section = acMatch.Value;
            var numbered = NumberedCriterion().Matches(section);
            foreach (Match m in numbered)
            {
                criteria.Add(new AcceptanceCriterion(
                    int.Parse(m.Groups[1].Value),
                    m.Groups[2].Value.Trim(),
                    m.Groups[3].Value.Trim()));
            }
        }

        // Pattern B: Checkbox items under "## Verification" (bug/security specs)
        if (criteria.Count == 0)
        {
            var verifyMatch = VerificationSection().Match(specContent);
            if (verifyMatch.Success)
            {
                var section = verifyMatch.Value;
                var checkboxes = CheckboxCriterion().Matches(section);
                var idx = 1;
                foreach (Match m in checkboxes)
                {
                    criteria.Add(new AcceptanceCriterion(idx++, m.Groups[1].Value.Trim(), ""));
                }
            }
        }

        return criteria;
    }

    [GeneratedRegex(@"## Acceptance Criteria.*?(?=\n## |\z)", RegexOptions.Singleline)]
    private static partial Regex AcceptanceCriteriaSection();

    [GeneratedRegex(@"(\d+)\.\s+\*\*([^*]+)\*\*\s*((?:\s+-\s+[^\n]+\n?)+)", RegexOptions.Multiline)]
    private static partial Regex NumberedCriterion();

    [GeneratedRegex(@"## Verification.*?(?=\n## |\z)", RegexOptions.Singleline)]
    private static partial Regex VerificationSection();

    [GeneratedRegex(@"-\s+\[\s*\]\s+(.+)")]
    private static partial Regex CheckboxCriterion();

    // Verification defaults (no config needed)
    const int MaxParallelAgents = 4;
    const int VerifyTimeoutMs = 300000; // 5 minutes

    /// <summary>
    /// Run criterion-based verification. Tests are run once by CLI before calling this.
    /// </summary>
    static async Task<(bool AllPassed, List<CriterionResult> Results, string? FailureContext)> RunCriterionVerification(
        string specContent, string specRef, List<AcceptanceCriterion> criteria)
    {
        var promptPath = Path.Combine(FindMillHome(), "run/prompts/loop-verify-criterion.md");
        var template = await File.ReadAllTextAsync(promptPath);

        var results = new List<CriterionResult>();

        // Batch criteria by MaxParallelAgents
        var batches = criteria
            .Select((c, i) => (Criterion: c, Batch: i / MaxParallelAgents))
            .GroupBy(x => x.Batch)
            .Select(g => g.Select(x => x.Criterion).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            if (batch.Count > 1)
                Out.Detail($"verifying criteria {batch.First().Index}-{batch.Last().Index} in parallel...");
            else
                Out.Detail($"verifying criterion {batch.First().Index}...");

            var tasks = batch.Select(async criterion =>
            {
                var rendered = template
                    .Replace("{{SPEC_CONTENT}}", specContent)
                    .Replace("{{SPEC_REF}}", specRef)
                    .Replace("{{CRITERION_INDEX}}", criterion.Index.ToString())
                    .Replace("{{TOTAL_CRITERIA}}", criteria.Count.ToString())
                    .Replace("{{CRITERION_TITLE}}", criterion.Title)
                    .Replace("{{CRITERION_BODY}}", criterion.Body);

                try
                {
                    using var cts = new CancellationTokenSource(VerifyTimeoutMs);
                    var output = await RunClaudeBatchWithTimeout(rendered, cts.Token);
                    return ParseCriterionResult(output, criterion);
                }
                catch (OperationCanceledException)
                {
                    return new CriterionResult(criterion.Index, criterion.Title, false,
                        "Verification timed out", "Criterion may be too complex to verify");
                }
            });

            var batchResults = await Task.WhenAll(tasks);
            results.AddRange(batchResults);

            // Early exit on failure
            if (batchResults.Any(r => !r.Passed))
                break;
        }

        var allPassed = results.All(r => r.Passed);
        var failures = results.Where(r => !r.Passed).ToList();

        string? failureContext = null;
        if (!allPassed)
        {
            var blockers = failures.Select(f => $"[{f.Index}] {f.Title}: {f.Reason}");
            var suggestion = failures.FirstOrDefault()?.Suggestion ?? "Address the failed criteria";
            failureContext = $"Blockers: {string.Join("; ", blockers)}\nSuggestion: {suggestion}";
        }

        return (allPassed, results, failureContext);
    }

    /// <summary>
    /// Run Claude batch with cancellation token for timeout.
    /// </summary>
    static async Task<string> RunClaudeBatchWithTimeout(string input, CancellationToken ct)
    {
        var cli = RequireCli();
        if (cli == null) return "";

        var output = new System.Text.StringBuilder();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cli,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        process.StartInfo.ArgumentList.Add("-p");
        process.StartInfo.ArgumentList.Add("--dangerously-skip-permissions");
        process.StartInfo.ArgumentList.Add("--verbose");
        process.StartInfo.ArgumentList.Add("--output-format");
        process.StartInfo.ArgumentList.Add("stream-json");

        process.Start();

        // Register cancellation to kill process
        ct.Register(() => { try { process.Kill(); } catch { } });

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        string? line;
        while ((line = await process.StandardOutput.ReadLineAsync(ct)) is not null)
        {
            if (line.Length == 0) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var msgType) && msgType.GetString() == "assistant" &&
                    root.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in content.EnumerateArray())
                    {
                        if (item.TryGetProperty("type", out var type) && type.GetString() == "text" &&
                            item.TryGetProperty("text", out var text))
                        {
                            output.AppendLine(text.GetString() ?? "");
                        }
                    }
                }
            }
            catch (JsonException)
            {
                output.AppendLine(line);
            }
        }

        await process.WaitForExitAsync(ct);
        return output.ToString();
    }

    static CriterionResult ParseCriterionResult(string output, AcceptanceCriterion criterion)
    {
        if (output.Contains("CRITERION_PASS"))
        {
            return new CriterionResult(criterion.Index, criterion.Title, true, null, null);
        }
        else if (output.Contains("CRITERION_FAIL"))
        {
            try
            {
                var failIndex = output.IndexOf("CRITERION_FAIL");
                var afterToken = output[(failIndex + "CRITERION_FAIL".Length)..];
                var jsonStart = afterToken.IndexOf('{');
                var jsonEnd = afterToken.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = afterToken[jsonStart..(jsonEnd + 1)];
                    var fail = JsonSerializer.Deserialize(json, CriterionJsonContext.Default.CriterionFail);
                    if (fail != null)
                        return new CriterionResult(criterion.Index, criterion.Title, false, fail.Reason, fail.Suggestion);
                }
            }
            catch { }
            return new CriterionResult(criterion.Index, criterion.Title, false, "Criterion not met", null);
        }
        return new CriterionResult(criterion.Index, criterion.Title, false, "No verification token in output", null);
    }

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

    static string? ExtractAbortReason(string output)
    {
        // Look for "MILL_ABORT" followed by reason text
        // Format: MILL_ABORT\n\n<reason> or MILL_ABORT: <reason>
        var abortIndex = output.IndexOf("MILL_ABORT");
        if (abortIndex < 0) return null;

        var afterToken = output[(abortIndex + "MILL_ABORT".Length)..].TrimStart();

        // Check for colon format: MILL_ABORT: reason
        if (afterToken.StartsWith(':'))
        {
            var colonReason = afterToken[1..].Trim();
            var endOfLine = colonReason.IndexOf('\n');
            return endOfLine > 0 ? colonReason[..endOfLine].Trim() : colonReason;
        }

        // Otherwise take the next non-empty line as the reason
        var lines = afterToken.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 ? lines[0].Trim() : null;
    }

    static string FindMillHome()
    {
        // 1. Explicit override via environment variable
        var env = Environment.GetEnvironmentVariable("MILL_HOME");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(Path.Combine(env, "spec/prompts")))
            return env;

        // 2. System install location (~/.local/share/mill or %LOCALAPPDATA%\mill)
        var dataPath = Installer.GetDataPath();
        if (Directory.Exists(Path.Combine(dataPath, "spec/prompts")))
            return dataPath;

        // 3. Development mode: prompts in repo relative to binary
        var exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var millRoot = Directory.GetParent(exeDir)?.FullName;
        if (millRoot != null && Directory.Exists(Path.Combine(millRoot, "spec/prompts")))
            return millRoot;

        // 4. Fallback to exe directory (will likely fail, but provides useful error)
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
