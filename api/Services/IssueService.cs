using MillApi.Models;
using MillApi.Services.Providers;

namespace MillApi.Services;

/// <summary>
/// Service for issues via provider abstraction.
/// </summary>
public class IssueService
{
    private readonly ILogger<IssueService> _logger;
    private readonly ProjectContext _project;
    private readonly IssueProviderFactory _providerFactory;

    public IssueService(
        ILogger<IssueService> logger,
        ProjectContext project,
        IssueProviderFactory providerFactory)
    {
        _logger = logger;
        _project = project;
        _providerFactory = providerFactory;
    }

    public async Task<List<Issue>> GetAll(bool forceRefresh = false)
    {
        var provider = await _providerFactory.GetProvider(_project.ProjectPath);
        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", _project.ProjectPath);
            return [];
        }

        return await provider.GetIssues(_project.ProjectPath, forceRefresh);
    }

    public async Task<IssueDetail?> Get(int number, bool forceRefresh = false)
    {
        var provider = await _providerFactory.GetProvider(_project.ProjectPath);
        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", _project.ProjectPath);
            return null;
        }

        return await provider.GetIssueDetail(number, _project.ProjectPath, forceRefresh);
    }

    public async Task<IssueCloseResult> Close(int number)
    {
        var provider = await _providerFactory.GetProvider(_project.ProjectPath);
        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", _project.ProjectPath);
            return new IssueCloseResult(false, "No issue provider available for this repository.");
        }

        var result = await provider.CloseIssue(number, _project.ProjectPath);
        if (result.Closed)
        {
            provider.ClearCache();
        }

        return result;
    }

    public async Task<IssueCreateResult> Create(string title, string body, string label)
    {
        var provider = await _providerFactory.GetProvider(_project.ProjectPath);
        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", _project.ProjectPath);
            return new IssueCreateResult(false, Error: "No issue provider available for this repository.");
        }

        var result = await provider.CreateIssue(title, body, label, _project.ProjectPath);
        if (result.Success)
        {
            provider.ClearCache();
        }

        return result;
    }

    public async Task ClearCache()
    {
        var provider = await _providerFactory.GetProvider(_project.ProjectPath);
        provider?.ClearCache();
    }

    public async Task<string?> GetProviderName()
    {
        var provider = await _providerFactory.GetProvider(_project.ProjectPath);
        return provider?.Name;
    }

    public async Task<PrCreateResult> CreatePullRequest(int issueNumber, string branch, string title, string body)
    {
        var provider = await _providerFactory.GetProvider(_project.ProjectPath);
        if (provider == null)
        {
            _logger.LogWarning("No issue provider available for {WorkingDir}", _project.ProjectPath);
            return new PrCreateResult(false, Error: "No issue provider available for this repository.");
        }

        var result = await provider.CreatePullRequest(issueNumber, branch, title, body, _project.ProjectPath);
        if (result.Success)
        {
            provider.ClearCache();
        }

        return result;
    }
}
