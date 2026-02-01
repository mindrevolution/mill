using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
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
        // Configure JSON to use string enums
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        // Core
        services.AddSingleton<MillPaths>();
        services.AddSingleton<ProjectContext>();
        services.AddSingleton<MarkdownService>();

        // Providers
        services.AddSingleton<IIssueProvider, GithubProvider>();
        services.AddSingleton<IIssueProvider, GitlabProvider>();
        services.AddSingleton<IssueProviderFactory>();
        services.AddSingleton<ILlmProvider, ClaudeCodeProvider>();

        // Domain services
        services.AddSingleton<GroundService>();
        services.AddSingleton<ObservationService>();
        services.AddSingleton<DraftService>();
        services.AddSingleton<BriefService>();
        services.AddSingleton<IssueService>();
        services.AddSingleton<ShipService>();

        // Job queue
        services.AddSingleton<JobService>();

        return services;
    }

    /// <summary>
    /// Map mill API endpoints to the application.
    /// </summary>
    public static WebApplication MapMillApi(this WebApplication app)
    {
        app.MapProjectEndpoints();
        app.MapGroundEndpoints();
        app.MapKnowledgeEndpoints();
        app.MapBriefEndpoints();
        app.MapSpecEndpoints();
        app.MapRunEndpoints();
        app.MapHistoryEndpoints();
        app.MapJobEndpoints();

        return app;
    }
}
