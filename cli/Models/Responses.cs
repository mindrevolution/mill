namespace Mill.Models;

// Shared JSON response records for AOT-compatible serialization.
// Replaces anonymous types used across commands.

/// <summary>Error with optional identifying fields.</summary>
public record ErrorResponse(string Error, string? Id = null, string? Category = null, string? Type = null, string? Slug = null);

/// <summary>Context show response.</summary>
public record ContextShowResponse(bool Exists, string? Path = null, string? Content = null, DateTime? UpdatedAt = null);

/// <summary>Context status response.</summary>
public record ContextStatusResponse(bool Exists, string Freshness, int CommitsBehind, string? Path, DateTime? UpdatedAt, string? CommitHash, string? CurrentHash);

/// <summary>Ground get response.</summary>
public record GroundGetResponse(string Category, string Id, string Content);

/// <summary>Ground create response.</summary>
public record GroundCreateResponse(bool Created, string Category, string Id, string Path);

/// <summary>Init response.</summary>
public record InitResponse(bool Initialized, string Path, string[]? Created = null);

/// <summary>Draft validate (no result) response.</summary>
public record DraftNoValidationResponse(bool HasValidation, string Slug);

/// <summary>History add response.</summary>
public record HistoryAddResponse(bool Added, HistoryEntry Entry);
