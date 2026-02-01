namespace MillApi.Models;

// Project
public record Project(string Name, string Path, DateTime LastOpened);

// Drafts & Issues
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
    string BodyHtml,
    string Type,
    string Status,
    DateTime UpdatedAt,
    string? Persona,
    bool HasRelevance = false
);

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
    string TitleHtml,
    string Body,
    string BodyHtml,
    string Type,
    string Status,
    string? Persona,
    List<string> Labels,
    DateTime CreatedAt
);

// Knowledge
public record KnowledgeItem(
    string Id,
    string Category,
    string Name,
    string Description,
    string File,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record Observation(
    string Id,
    string Category,
    string Suggestion,
    List<string> Sources,
    double Confidence,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// Runs
public record Run(
    string Id,
    int Issue,
    string Title,
    string Status,
    int Iteration,
    int MaxIterations,
    DateTime StartedAt
);

public record RunLog(string Id, List<string> Lines);

// History
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
    string? PrUrl = null
);

// Spec Chat
public record SpecSession(string Id, string? DraftId, DateTime StartedAt);

public record ChatMessage(string Role, string Content, DateTime Timestamp);

public record SendMessageRequest(string SessionId, string Content);

public record SendMessageResponse(string Content, Draft? UpdatedDraft);

// Briefs
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

public record CreateBriefRequest(string Title, string Intent, string? Type);
public record UpdateBriefRequest(string? Title, string? Stage, string? Intent, string? Persona, List<string>? Concepts, string? Content);
public record DropBriefRequest(string Essence);
public record PromoteBriefRequest(string Type);

// Requests
public record StartSpecRequest(string? DraftId, string? Type);

public record StartRunRequest(int Issue);

// Ground/Kickstart
public record GroundStatus(bool IsEmpty, bool HasKickstart);

public record TemplateSummary(string Id, string Label, string Summary, List<string> Tags);

public record TemplateContent(string Id, string Type, string Content);

public record KickstartRequest(
    string Name,
    string? Description,
    string ArchetypeId,
    string StackId,
    ArchetypeOverride? ArchetypeOverride,
    StackOverride? StackOverride
);

public record ArchetypeOverride(
    string? Type,
    string? PrimaryUser,
    string? CoreLoop,
    string? SuccessMetric,
    string? Monetization
);

public record StackOverride(
    string? Frontend,
    string? Backend,
    string? DataStorage,
    string? InfraDeploy,
    string? Testing,
    string? Observability,
    string? Avoid
);

public record KickstartResponse(List<CreatedFile> Created);

public record CreatedFile(string Category, string Id, string Path);

// LLM Provider
public record LlmResponse(bool Success, string Output, string? Error);

public record LlmOptions(string? WorkingDir = null, int TimeoutMs = 120000, string? SystemPrompt = null);

// Draft Validation
public record DraftValidationRequest(string DraftId);

public record DraftValidationResponse(
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

// Job Queue
public enum JobType { DraftValidation, ContextWarmup, Kickstart, ShipRun, ObservationExtraction }
public enum JobStatus { Queued, Running, Completed, Failed, Cancelled }
public enum JobQueue { Llm, Ship }

// Ship Run
public record ShipRunResult(
    bool Success,
    int Iterations,
    string? PrUrl,
    string? AbortReason,
    List<ShipRunIteration> History
);

public record ShipRunIteration(
    int Number,
    string Signal,
    string? Summary,
    DateTime CompletedAt
);

// Observation Extraction
public record ObservationExtractionParams(
    int IssueNumber,
    string IssueTitle,
    string SpecContent,
    bool RunSuccess,
    int Iterations,
    List<ShipRunIteration> IterationHistory
);

public record ObservationExtractionResult(
    int Extracted,
    int Added,
    int Updated,
    int Discarded
);

public record Job(
    string Id,
    JobType Type,
    JobQueue Queue,
    JobStatus Status,
    string Title,
    Dictionary<string, object?> Params,
    string? Stage,
    int? ProgressPercent,
    object? Result,
    string? Error,
    string? SourceWorkspace,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public record CreateJobRequest(
    JobType Type,
    Dictionary<string, object?> Params,
    string? SourceWorkspace
);

public record JobCreatedResponse(string Id);

public record JobEvent(string EventType, Job Job);

// API Response types
public record ProjectResponse(bool Configured, string? Path = null, string? Name = null, string? IssueProvider = null);
public record ProjectSetResponse(string Path, string Name);
public record KnowledgeItemContent(string Id, string Category, string Content);
public record IssueNotFoundError(string Error);
public record AbortedResponse(string Aborted);
