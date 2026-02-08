namespace Mill.Models;

/// <summary>
/// Summary for list command.
/// </summary>
public record Observation(
    string Id,
    string Source,      // ship | spec | warmup | ground
    string Type,        // extraction | discovery | concern | suggestion
    string Title,
    DateOnly Created,
    int? Issue          // optional related issue
);

/// <summary>
/// Full detail for get command.
/// </summary>
public record ObservationDetail(
    string Id,
    string Source,
    string Type,
    string Title,
    string Content,     // markdown body
    DateOnly Created,
    int? Issue
);

/// <summary>
/// List response wrapper.
/// </summary>
public record ObservationsList(
    List<Observation> Observations,
    int Count
);
