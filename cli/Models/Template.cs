namespace Mill.Models;

public record TemplateSummary(
    string Id,
    string Label,
    string Summary,
    List<string> Tags
);

public record TemplateContent(
    string Id,
    string Type,
    string Content
);
