using System.Text.Json;
using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class JobEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs");

        group.MapPost("/", (CreateJobRequest request, JobService jobs) =>
        {
            var id = jobs.Enqueue(request);
            return Results.Ok(new JobCreatedResponse(id));
        })
        .WithName("CreateJob")
        .WithSummary("Create a new background job");

        group.MapGet("/", (JobService jobs) =>
        {
            var list = jobs.GetAll();
            return Results.Ok(list);
        })
        .WithName("GetJobs")
        .WithSummary("List all jobs");

        group.MapGet("/{id}", (string id, JobService jobs) =>
        {
            var job = jobs.Get(id);
            return job != null ? Results.Ok(job) : Results.NotFound();
        })
        .WithName("GetJob")
        .WithSummary("Get a single job");

        group.MapDelete("/{id}", (string id, JobService jobs) =>
        {
            var cancelled = jobs.Cancel(id);
            return cancelled ? Results.Ok() : Results.NotFound();
        })
        .WithName("CancelJob")
        .WithSummary("Cancel a job");

        group.MapDelete("/", (JobService jobs) =>
        {
            jobs.ClearCompleted();
            return Results.Ok();
        })
        .WithName("ClearJobs")
        .WithSummary("Clear completed/failed/cancelled jobs");

        // SSE endpoint for job events
        group.MapGet("/events", async (JobService jobs, HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var channel = jobs.Subscribe();

            try
            {
                // Send initial state
                foreach (var job in jobs.GetAll())
                {
                    var data = JsonSerializer.Serialize(job, JsonOptions);
                    await ctx.Response.WriteAsync($"event: job-sync\ndata: {data}\n\n", ct);
                }
                await ctx.Response.Body.FlushAsync(ct);

                // Stream updates
                await foreach (var evt in channel.Reader.ReadAllAsync(ct))
                {
                    var data = JsonSerializer.Serialize(evt.Job, JsonOptions);
                    await ctx.Response.WriteAsync($"event: {evt.EventType}\ndata: {data}\n\n", ct);
                    await ctx.Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
        })
        .WithName("JobEvents")
        .WithSummary("SSE stream of job events");

        // Config endpoint
        group.MapGet("/config", (JobService jobs) =>
        {
            return Results.Ok(new
            {
                llmConcurrency = jobs.LlmConcurrency,
                shipConcurrency = jobs.ShipConcurrency
            });
        })
        .WithName("GetJobConfig")
        .WithSummary("Get job queue configuration");
    }
}
