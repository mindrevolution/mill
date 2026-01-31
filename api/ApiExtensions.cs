using MillApi.Endpoints;
using MillApi.Services;
using MillApi.Services.Providers;

namespace MillApi;

/// <summary>
/// Extension methods for integrating the mill API into a host application.
/// </summary>
public static class ApiExtensions
{
    /// <summary>
    /// Add mill API services to the service collection.
    /// </summary>
    public static IServiceCollection AddMillApi(this IServiceCollection services)
    {
        services.AddSingleton<MarkdownService>();
        services.AddSingleton<IIssueProvider, GithubProvider>();
        services.AddSingleton<IIssueProvider, GitlabProvider>();
        services.AddSingleton<IssueProviderFactory>();
        services.AddSingleton<MillService>();

        return services;
    }

    /// <summary>
    /// Map mill API endpoints to the application.
    /// </summary>
    public static WebApplication MapMillApi(this WebApplication app)
    {
        app.MapProjectEndpoints();
        app.MapKnowledgeEndpoints();
        app.MapBriefEndpoints();
        app.MapSpecEndpoints();
        app.MapRunEndpoints();
        app.MapHistoryEndpoints();

        return app;
    }
}
