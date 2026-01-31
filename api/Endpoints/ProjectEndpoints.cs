using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/project")
            .WithTags("Project");

        group.MapGet("/", async (ProjectContext project, IssueService issues) =>
        {
            var path = project.GetProjectPath();
            if (string.IsNullOrEmpty(path))
                return Results.Ok(new ProjectResponse(false));

            var provider = await issues.GetProviderName();
            return Results.Ok(new ProjectResponse(true, path, Path.GetFileName(path), provider));
        })
        .WithName("GetProject")
        .WithSummary("Get current project info including detected issue provider");

        group.MapPost("/", (SetProjectRequest request, ProjectContext project) =>
        {
            if (!Directory.Exists(request.Path))
                return Results.BadRequest("Path does not exist");

            project.SetProjectPath(request.Path);
            return Results.Ok(new ProjectSetResponse(request.Path, Path.GetFileName(request.Path)));
        })
        .WithName("SetProject")
        .WithSummary("Set the active project path");

        group.MapGet("/config", async (ProjectContext project) =>
        {
            var path = project.GetProjectPath();
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
