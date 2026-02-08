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
