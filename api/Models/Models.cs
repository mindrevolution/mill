using System.Text.Json.Serialization;

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
    string Source,
    double Confidence,
    DateTime CreatedAt
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
    List<string>? ContextAdded
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

// API Response types (for AOT compatibility)
public record ProjectResponse(bool Configured, string? Path = null, string? Name = null, string? IssueProvider = null);
public record ProjectSetResponse(string Path, string Name);
public record KnowledgeItemContent(string Id, string Category, string Content);
public record IssueNotFoundError(string Error);
public record AbortedResponse(string Aborted);

// JSON serialization context (AOT-compatible)
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(Draft))]
[JsonSerializable(typeof(List<Draft>))]
[JsonSerializable(typeof(Issue))]
[JsonSerializable(typeof(List<Issue>))]
[JsonSerializable(typeof(IssueDetail))]
[JsonSerializable(typeof(KnowledgeItem))]
[JsonSerializable(typeof(List<KnowledgeItem>))]
[JsonSerializable(typeof(Observation))]
[JsonSerializable(typeof(Run))]
[JsonSerializable(typeof(List<Run>))]
[JsonSerializable(typeof(RunLog))]
[JsonSerializable(typeof(HistoryEntry))]
[JsonSerializable(typeof(List<HistoryEntry>))]
[JsonSerializable(typeof(SpecSession))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(SendMessageRequest))]
[JsonSerializable(typeof(SendMessageResponse))]
[JsonSerializable(typeof(StartSpecRequest))]
[JsonSerializable(typeof(StartRunRequest))]
[JsonSerializable(typeof(ProjectResponse))]
[JsonSerializable(typeof(ProjectSetResponse))]
[JsonSerializable(typeof(KnowledgeItemContent))]
[JsonSerializable(typeof(IssueNotFoundError))]
[JsonSerializable(typeof(AbortedResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class ApiJsonContext : JsonSerializerContext { }
