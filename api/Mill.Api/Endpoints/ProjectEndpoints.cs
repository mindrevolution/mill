using Mill.Api.Services;

namespace Mill.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/project")
            .WithTags("Project");

        group.MapGet("/", (MillService mill) =>
        {
            var path = mill.GetProjectPath();
            if (string.IsNullOrEmpty(path))
                return Results.Ok(new { configured = false });

            return Results.Ok(new
            {
                configured = true,
                path,
                name = Path.GetFileName(path)
            });
        })
        .WithName("GetProject")
        .WithSummary("Get current project info");

        group.MapPost("/", (SetProjectRequest request, MillService mill) =>
        {
            if (!Directory.Exists(request.Path))
                return Results.BadRequest("Path does not exist");

            mill.SetProjectPath(request.Path);
            return Results.Ok(new
            {
                path = request.Path,
                name = Path.GetFileName(request.Path)
            });
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
