using System.Text.Json.Serialization;

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

/// <summary>Top-level NDJSON line from claude --output-format stream-json</summary>
internal record StreamEvent(string? Type, string? Subtype, StreamEventData? Event, string? Result);

/// <summary>Nested event data within a stream event</summary>
internal record StreamEventData(
    string? Type,
    [property: JsonPropertyName("content_block")] StreamContentBlock? ContentBlock,
    StreamDelta? Delta
);

/// <summary>Tool use content block within a stream event</summary>
internal record StreamContentBlock(string? Type, string? Name);

/// <summary>Delta within a content_block_delta event</summary>
internal record StreamDelta(string? Type, string? Text);
