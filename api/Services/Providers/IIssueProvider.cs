using MillApi.Models;

namespace MillApi.Services.Providers;

/// <summary>
/// Interface for issue tracking providers (GitHub, GitLab, etc.)
/// </summary>
public interface IIssueProvider
{
    /// <summary>
    /// Provider identifier (e.g., "github", "gitlab")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// List open issues from the provider.
    /// Implementations may cache results; use forceRefresh to bypass cache.
    /// </summary>
    Task<List<Issue>> GetIssues(string workingDir, bool forceRefresh = false);

    /// <summary>
    /// Get full details for a single issue.
    /// Implementations may cache results; use forceRefresh to bypass cache.
    /// </summary>
    Task<IssueDetail?> GetIssueDetail(int number, string workingDir, bool forceRefresh = false);

    /// <summary>
    /// Close an issue in the provider.
    /// </summary>
    Task<IssueCloseResult> CloseIssue(int number, string workingDir);

    /// <summary>
    /// Create a pull request that closes the given issue.
    /// </summary>
    Task<PrCreateResult> CreatePullRequest(int issueNumber, string branch, string title, string body, string workingDir);

    /// <summary>
    /// Check if this provider can handle the given repository
    /// </summary>
    Task<bool> CanHandle(string workingDir);

    /// <summary>
    /// Clear any cached data. Default implementation does nothing.
    /// </summary>
    void ClearCache() { }
}

public record IssueCloseResult(bool Closed, string? Error = null);
public record PrCreateResult(bool Success, string? Url = null, string? Error = null);
