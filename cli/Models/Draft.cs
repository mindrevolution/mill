namespace Mill.Models;

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
