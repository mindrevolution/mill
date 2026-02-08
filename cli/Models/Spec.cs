namespace Mill.Models;

public record Spec(
    int Number,
    string Title,
    string Type,
    string Status,
    string? Persona,
    DateTime CreatedAt
);

public record SpecDetail(
    int Number,
    string Title,
    string Body,
    string Type,
    string Status,
    string? Persona,
    List<string> Labels,
    DateTime CreatedAt
);

// GitHub CLI JSON models (used by SpecCommand for gh output deserialization)
internal record GhIssueListItem(int Number, string Title, List<GhLabel>? Labels, DateTime CreatedAt);
internal record GhIssueViewItem(int Number, string Title, string? Body, string State, List<GhLabel>? Labels, DateTime CreatedAt);
internal record GhLabel(string Name);
