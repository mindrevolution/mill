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
}
