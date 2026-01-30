using Mill.Api.Models;

namespace Mill.Api.Services.Providers;

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
    /// List open issues from the provider
    /// </summary>
    Task<List<Issue>> GetIssues(string workingDir);

    /// <summary>
    /// Get full details for a single issue
    /// </summary>
    Task<IssueDetail?> GetIssueDetail(int number, string workingDir);

    /// <summary>
    /// Check if this provider can handle the given repository
    /// </summary>
    Task<bool> CanHandle(string workingDir);
}
