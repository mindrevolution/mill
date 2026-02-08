namespace Mill.Models;

public record Idea(
    string Id,
    string Title,
    string Stage,
    string Intent,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Persona,
    List<string>? Concepts
);

public record IdeaDetail(
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

public record DroppedIdea(
    string Id,
    string Essence,
    DateTime DroppedAt,
    string OriginalTitle
);

internal record DroppedIdeasFile(List<DroppedIdea> Dropped);
