namespace Mill.Models;

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

internal record HistoryFile(List<HistoryEntry> Runs);
