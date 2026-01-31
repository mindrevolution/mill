using System.Text.Json.Serialization;

// =============================================================================
// Install types
// =============================================================================

/// <summary>
/// Install mode: fresh install (copy to system) vs update (download latest).
/// </summary>
enum InstallMode { Install, Update }

/// <summary>
/// Install/update check result.
/// </summary>
record InstallCheck(
    InstallMode Mode,
    string Current,
    string? Latest,
    string? InstalledVersion,
    bool UpdateAvailable,
    bool IsDowngrade,
    string? Error);

// =============================================================================
// GitHub API types
// =============================================================================

record GhIssue(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("labels")] List<GhLabel> Labels,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt);

record GhIssueDetail(
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("labels")] List<GhLabel> Labels);

record GhIssueDetailFull(
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("labels")] List<GhLabel> Labels);

record GhLabel([property: JsonPropertyName("name")] string Name);

record GhRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("assets")] List<GhAsset> Assets);

record GhAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);

// =============================================================================
// mill configuration types
// =============================================================================

record MillConfig
{
    public List<string> Exclude { get; init; } = ["backlog", "deferred", "on-hold", "wontfix", "duplicate", "invalid"];
    public ScoringConfig Scoring { get; init; } = new();
    public HealthConfig Health { get; init; } = new();
    public ContextConfig Context { get; init; } = new();
}

record ContextConfig
{
    /// <summary>
    /// Number of commits before context is considered stale.
    /// Context is rebuilt only when HEAD is more than this many commits ahead.
    /// </summary>
    public int StaleThreshold { get; init; } = 25;
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

// =============================================================================
// Verification types
// =============================================================================

record VerifyMetadata(
    [property: JsonPropertyName("branch")] string Branch,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("verification")] string Verification);

record VerificationResult(bool Passed, string? FailureContext);
record PullRequestResult(bool Success, string? Error);

record AcceptanceCriterion(int Index, string Title, string Body);

record CriterionResult(int Index, string Title, bool Passed, string? Reason, string? Suggestion);

record CriterionPass(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("title")] string Title);

record CriterionFail(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("suggestion")] string? Suggestion);

// =============================================================================
// Spec and worktree types
// =============================================================================

record SpecInfo(string Content, string Ref, string IssueNumber, bool IsGhIssue);
record SpecLoadResult(SpecInfo? Spec, int? ExitCode);

record WorktreeInfo(string Path, string BranchName, string OriginalDir);
record WorktreeSetupResult(WorktreeInfo? Worktree, int? ExitCode);

// =============================================================================
// Sweep analysis types
// =============================================================================

record SweepFinding(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("file")] string? File,
    [property: JsonPropertyName("line")] int? Line,
    [property: JsonPropertyName("severity")] string Severity);

record SweepCategory(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("findings")] List<SweepFinding> Findings);

record SweepAnalysis(
    [property: JsonPropertyName("categories")] List<SweepCategory> Categories);

