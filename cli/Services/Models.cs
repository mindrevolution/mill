using System.Text.Json.Serialization;

namespace Mill.Services;

// ─────────────────────────────────────────────────────────────────────────────
// Ground
// ─────────────────────────────────────────────────────────────────────────────

public record KnowledgeItem(
    string Id,
    string Category,
    string Name,
    string Description,
    string File,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ─────────────────────────────────────────────────────────────────────────────
// Brief
// ─────────────────────────────────────────────────────────────────────────────

public record Brief(
    string Id,
    string Title,
    string Stage,
    string Intent,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Persona,
    List<string>? Concepts
);

public record BriefDetail(
    string Id,
    string Title,
    string Stage,
    string Intent,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Persona,
    List<string>? Concepts
);

public record DroppedBrief(
    string Id,
    string Essence,
    DateTime DroppedAt,
    string OriginalTitle
);

// ─────────────────────────────────────────────────────────────────────────────
// Draft
// ─────────────────────────────────────────────────────────────────────────────

public record Draft(
    string Id,
    string Slug,
    string Title,
    string Type,
    string Status,
    DateTime UpdatedAt,
    string? Persona
);

public record DraftDetail(
    string Id,
    string Slug,
    string Title,
    string Body,
    string Type,
    string Status,
    DateTime UpdatedAt,
    string? Persona,
    bool HasRelevance = false
);

public record DraftPublishResult(
    bool Success,
    int? Number = null,
    string? Url = null,
    string? Error = null
);

public record DraftValidationResult(
    int Score,
    string Verdict,
    List<ValidationFinding> Findings,
    string Summary,
    string Recommendation
);

public record ValidationFinding(
    string Category,
    string Severity,
    string Reference,
    string Expected,
    string Actual,
    string Impact
);

// ─────────────────────────────────────────────────────────────────────────────
// Issue
// ─────────────────────────────────────────────────────────────────────────────

public record Issue(
    int Number,
    string Title,
    string Type,
    string Status,
    string? Persona,
    DateTime CreatedAt
);

public record IssueDetail(
    int Number,
    string Title,
    string Body,
    string Type,
    string Status,
    string? Persona,
    List<string> Labels,
    DateTime CreatedAt
);

// ─────────────────────────────────────────────────────────────────────────────
// History
// ─────────────────────────────────────────────────────────────────────────────

public record HistoryEntry(
    DateTime Date,
    int Issue,
    int? Pr,
    string Title,
    string Type,
    string? Persona,
    string Intent,
    string Outcome,
    List<string>? ContextAdded,
    int? Iterations = null,
    long? DurationMs = null,
    string? PrUrl = null,
    string? GitUser = null
);

// ─────────────────────────────────────────────────────────────────────────────
// Templates
// ─────────────────────────────────────────────────────────────────────────────

public record TemplateSummary(
    string Id,
    string Label,
    string Summary,
    List<string> Tags
);

public record TemplateContent(
    string Id,
    string Type,
    string Content
);

// ─────────────────────────────────────────────────────────────────────────────
// Config
// ─────────────────────────────────────────────────────────────────────────────

public record ProjectConfig
{
    public List<string> Exclude { get; init; } = ["backlog", "deferred", "on-hold", "wontfix", "duplicate", "invalid"];
    public ContextConfig Context { get; init; } = new();
}

public record ContextConfig
{
    /// <summary>
    /// Number of commits before context is considered stale.
    /// </summary>
    public int StaleThreshold { get; init; } = 25;
}

// ─────────────────────────────────────────────────────────────────────────────
// JSON file wrappers
// ─────────────────────────────────────────────────────────────────────────────

internal record DroppedBriefsFile(List<DroppedBrief> Dropped);
internal record HistoryFile(List<HistoryEntry> Runs);
