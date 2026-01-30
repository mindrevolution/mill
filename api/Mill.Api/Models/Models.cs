namespace Mill.Api.Models;

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
    string Body,
    string BodyHtml,
    string Type,
    string Status,
    string? Persona,
    List<string> Labels,
    DateTime CreatedAt
);

// Library
public record LibraryItem(
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

// Requests
public record StartSpecRequest(string? DraftId, string? Type);

public record StartRunRequest(int Issue);
