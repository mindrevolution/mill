namespace Mill.Models;

public record KnowledgeItem(
    string Id,
    string Category,
    string Name,
    string Description,
    string File,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
