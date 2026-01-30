using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class RunEndpoints
{
    public static void MapRunEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/run")
            .WithTags("Run");

        group.MapGet("/", async (MillService mill) =>
        {
            var runs = await mill.GetActiveRuns();
            return Results.Json(runs, ApiJsonContext.Default.ListRun);
        })
        .WithName("GetActiveRuns")
        .WithSummary("List active runs");

        group.MapPost("/{issue:int}", async (int issue, MillService mill) =>
        {
            var run = await mill.StartRun(issue);
            if (run == null)
                return Results.NotFound($"Issue #{issue} not found");

            return Results.Json(run, ApiJsonContext.Default.Run);
        })
        .WithName("StartRun")
        .WithSummary("Start a run for an issue");

        group.MapDelete("/{runId}", (string runId, MillService mill) =>
        {
            // TODO: Abort a running run
            return Results.Json(new AbortedResponse(runId), ApiJsonContext.Default.AbortedResponse);
        })
        .WithName("AbortRun")
        .WithSummary("Abort an active run");

        group.MapGet("/{runId}/logs", (string runId, MillService mill) =>
        {
            // TODO: Get logs for a run (could use SSE for streaming)
            return Results.Json(new RunLog(runId, ["• starting...", "✓ ready"]), ApiJsonContext.Default.RunLog);
        })
        .WithName("GetRunLogs")
        .WithSummary("Get logs for a run");
    }
}
