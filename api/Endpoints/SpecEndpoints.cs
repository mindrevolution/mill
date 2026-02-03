using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class SpecEndpoints
{
    public static void MapSpecEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/spec")
            .WithTags("Spec");

        group.MapGet("/drafts", async (DraftService drafts) =>
        {
            var list = await drafts.GetAll();
            return Results.Ok(list);
        })
        .WithName("GetDrafts")
        .WithSummary("List all spec drafts");

        group.MapGet("/drafts/{id}", async (string id, DraftService drafts) =>
        {
            var draft = await drafts.Get(id);
            return draft != null
                ? Results.Ok(draft)
                : Results.NotFound();
        })
        .WithName("GetDraftDetail")
        .WithSummary("Get single draft with full content");

        group.MapPost("/drafts/{id}/validate", async (string id, DraftService drafts) =>
        {
            try
            {
                var result = await drafts.ValidateRelevance(id);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        })
        .WithName("ValidateDraft")
        .WithSummary("Validate draft relevance against current codebase");

        group.MapGet("/drafts/{id}/relevance", async (string id, DraftService drafts) =>
        {
            var result = await drafts.GetRelevance(id);
            return result != null
                ? Results.Ok(result)
                : Results.NotFound();
        })
        .WithName("GetDraftRelevance")
        .WithSummary("Get cached relevance result for a draft");

        group.MapDelete("/drafts/{id}/relevance", (string id, DraftService drafts) =>
        {
            drafts.DeleteRelevance(id);
            return Results.Ok();
        })
        .WithName("DeleteDraftRelevance")
        .WithSummary("Delete cached relevance result for a draft");

        group.MapPost("/drafts/{id}/publish", async (string id, DraftService drafts, IssueService issues) =>
        {
            var result = await drafts.Publish(id, issues);
            return result.Success
                ? Results.Ok(new { number = result.Number, url = result.Url })
                : Results.Problem(result.Error ?? "Failed to publish draft", statusCode: 500);
        })
        .WithName("PublishDraft")
        .WithSummary("Publish a draft as a GitHub issue");

        group.MapGet("/issues", async (IssueService issues, bool refresh = false) =>
        {
            var list = await issues.GetAll(forceRefresh: refresh);
            return Results.Ok(list);
        })
        .WithName("GetIssues")
        .WithSummary("List GitHub issues (specs). Use ?refresh=true to bypass cache.");

        group.MapGet("/issues/{number:int}", async (int number, IssueService issues, bool refresh = false) =>
        {
            var issue = await issues.Get(number, forceRefresh: refresh);
            return issue != null
                ? Results.Ok(issue)
                : Results.Json(new IssueNotFoundError($"Issue #{number} not found"), statusCode: 404);
        })
        .WithName("GetIssueDetail")
        .WithSummary("Get single GitHub issue with full details. Use ?refresh=true to bypass cache.");

        group.MapPost("/issues/{number:int}/close", async (int number, IssueService issues) =>
        {
            var result = await issues.Close(number);
            return result.Closed
                ? Results.Ok(new { closed = true })
                : Results.Json(new { closed = false, error = result.Error ?? $"Failed to close issue #{number}" }, statusCode: 500);
        })
        .WithName("CloseIssue")
        .WithSummary("Close an issue on the configured provider.");

        group.MapPost("/start", (StartSpecRequest request) =>
        {
            // TODO: Start interactive spec session
            // This would spawn `mill spec --interactive` and manage stdin/stdout
            var sessionId = Guid.NewGuid().ToString("N")[..8];

            return Results.Ok(new SpecSession(
                Id: sessionId,
                DraftId: request.DraftId,
                StartedAt: DateTime.UtcNow
            ));
        })
        .WithName("StartSpec")
        .WithSummary("Start a new spec creation session");

        group.MapPost("/message", (SendMessageRequest request) =>
        {
            // TODO: Send message to active spec session
            // For now, return a placeholder response
            return Results.Ok(new SendMessageResponse(
                Content: "I understand you want to build something. Let me help you create a spec. What problem are you trying to solve?",
                UpdatedDraft: null
            ));
        })
        .WithName("SendSpecMessage")
        .WithSummary("Send a message in an active spec session");

        // SSE endpoint for draft file changes
        group.MapGet("/drafts/events", async (DraftWatcherService watcher, HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            var channel = watcher.Subscribe();

            try
            {
                // Send initial ping so client knows connection is established
                await ctx.Response.WriteAsync("event: connected\ndata: {}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);

                // Stream updates
                await foreach (var evt in channel.Reader.ReadAllAsync(ct))
                {
                    await ctx.Response.WriteAsync($"event: {evt.EventType}\ndata: {{}}\n\n", ct);
                    await ctx.Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
        })
        .WithName("DraftEvents")
        .WithSummary("SSE stream of draft file change events");
    }
}
