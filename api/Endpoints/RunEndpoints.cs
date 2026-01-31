using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class RunEndpoints
{
    public static void MapRunEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/run")
            .WithTags("Run");

        group.MapGet("/", async (ShipService ship) =>
        {
            var runs = await ship.GetActiveRuns();
            return Results.Ok(runs);
        })
        .WithName("GetActiveRuns")
        .WithSummary("List active runs");

        group.MapPost("/{issue:int}", async (int issue, ShipService ship) =>
        {
            var run = await ship.StartRun(issue);
            if (run == null)
                return Results.NotFound($"Issue #{issue} not found");

            return Results.Ok(run);
        })
        .WithName("StartRun")
        .WithSummary("Start a run for an issue");

        group.MapDelete("/{runId}", (string runId) =>
        {
            // TODO: Abort a running run
            return Results.Ok(new AbortedResponse(runId));
        })
        .WithName("AbortRun")
        .WithSummary("Abort an active run");

        group.MapGet("/{runId}/logs", (string runId) =>
        {
            // TODO: Get logs for a run (could use SSE for streaming)
            return Results.Ok(new RunLog(runId, ["• starting...", "✓ ready"]));
        })
        .WithName("GetRunLogs")
        .WithSummary("Get logs for a run");
    }
}
