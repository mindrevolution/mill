namespace Mill.Models;

public record ShipIteration(
    int Number,
    string Signal,
    string? Detail,
    DateTime Timestamp,
    long DurationMs
);

public record ShipResult(
    bool Success,
    int Issue,
    string Title,
    string Outcome,
    List<ShipIteration> Iterations,
    long DurationMs,
    string? PrUrl = null,
    int? PrNumber = null,
    string? AbortReason = null,
    HistoryEntry? History = null
);

/// <summary>Envelope from claude --output-format json</summary>
internal record ClaudeResponse(
    string? Result,
    string? Error,
    bool IsError = false
);

public record ParsedSignal(
    string Signal,
    string? Branch = null,
    string? Title = null,
    string? Summary = null,
    string? Done = null,
    string? Next = null,
    string? AbortReason = null,
    string? Verification = null,
    List<string>? Blockers = null,
    string? Suggestion = null
);

/// <summary>JSON payload after MILL_CONTINUE token</summary>
internal record ContinuePayload(string? Done, string? Next);

/// <summary>JSON payload after MILL_VERIFY token</summary>
internal record VerifyPayload(string? Branch, string? Title, string? Done, string? Summary, string? Verification);

/// <summary>JSON payload after MILL_REJECTED token</summary>
internal record RejectedPayload(List<string>? Blockers, string? Suggestion);
