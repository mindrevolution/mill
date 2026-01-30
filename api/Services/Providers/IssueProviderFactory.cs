namespace MillApi.Services.Providers;

/// <summary>
/// Factory that detects and creates the appropriate issue provider for a repository
/// </summary>
public class IssueProviderFactory
{
    private readonly IEnumerable<IIssueProvider> _providers;
    private readonly ILogger<IssueProviderFactory> _logger;

    public IssueProviderFactory(
        IEnumerable<IIssueProvider> providers,
        ILogger<IssueProviderFactory> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// Get the appropriate provider for the given working directory
    /// </summary>
    public async Task<IIssueProvider?> GetProvider(string workingDir)
    {
        foreach (var provider in _providers)
        {
            try
            {
                if (await provider.CanHandle(workingDir))
                {
                    _logger.LogDebug("Using {Provider} provider for {WorkingDir}", provider.Name, workingDir);
                    return provider;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if {Provider} can handle {WorkingDir}", provider.Name, workingDir);
            }
        }

        _logger.LogWarning("No issue provider found for {WorkingDir}", workingDir);
        return null;
    }

    /// <summary>
    /// Get a specific provider by name
    /// </summary>
    public IIssueProvider? GetProvider(string name, bool throwIfNotFound = false)
    {
        var provider = _providers.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (provider == null && throwIfNotFound)
            throw new InvalidOperationException($"Issue provider '{name}' not found");

        return provider;
    }

    /// <summary>
    /// List all available providers
    /// </summary>
    public IEnumerable<string> GetAvailableProviders() => _providers.Select(p => p.Name);
}
