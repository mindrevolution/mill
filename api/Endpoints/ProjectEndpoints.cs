using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/project")
            .WithTags("Project");

        group.MapGet("/", async (MillService mill) =>
        {
            var path = mill.GetProjectPath();
            if (string.IsNullOrEmpty(path))
                return Results.Json(new ProjectResponse(false), ApiJsonContext.Default.ProjectResponse);

            var provider = await mill.GetIssueProviderName();
            return Results.Json(
                new ProjectResponse(true, path, Path.GetFileName(path), provider),
                ApiJsonContext.Default.ProjectResponse);
        })
        .WithName("GetProject")
        .WithSummary("Get current project info including detected issue provider");

        group.MapPost("/", (SetProjectRequest request, MillService mill) =>
        {
            if (!Directory.Exists(request.Path))
                return Results.BadRequest("Path does not exist");

            mill.SetProjectPath(request.Path);
            return Results.Json(
                new ProjectSetResponse(request.Path, Path.GetFileName(request.Path)),
                ApiJsonContext.Default.ProjectSetResponse);
        })
        .WithName("SetProject")
        .WithSummary("Set the active project path");

        group.MapGet("/config", async (MillService mill) =>
        {
            var path = mill.GetProjectPath();
            if (string.IsNullOrEmpty(path))
                return Results.BadRequest("No project configured");

            var configPath = Path.Combine(path, ".mill", "project.json");
            if (!File.Exists(configPath))
                return Results.NotFound("No .mill/project.json found");

            var content = await File.ReadAllTextAsync(configPath);
            return Results.Content(content, "application/json");
        })
        .WithName("GetProjectConfig")
        .WithSummary("Get project configuration");
    }
}

public record SetProjectRequest(string Path);
