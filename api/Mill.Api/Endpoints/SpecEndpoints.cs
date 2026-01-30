using Mill.Api.Models;
using Mill.Api.Services;

namespace Mill.Api.Endpoints;

public static class SpecEndpoints
{
    public static void MapSpecEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/spec")
            .WithTags("Spec");

        group.MapGet("/drafts", async (MillService mill) =>
        {
            var drafts = await mill.GetDrafts();
            return Results.Ok(drafts);
        })
        .WithName("GetDrafts")
        .WithSummary("List all spec drafts");

        group.MapGet("/issues", async (MillService mill) =>
        {
            var issues = await mill.GetIssues();
            return Results.Ok(issues);
        })
        .WithName("GetIssues")
        .WithSummary("List GitHub issues (specs)");

        group.MapGet("/issues/{number:int}", async (int number, MillService mill) =>
        {
            var issue = await mill.GetIssueDetail(number);
            return issue != null
                ? Results.Ok(issue)
                : Results.NotFound(new { error = $"Issue #{number} not found" });
        })
        .WithName("GetIssueDetail")
        .WithSummary("Get single GitHub issue with full details");

        group.MapPost("/start", (StartSpecRequest request, MillService mill) =>
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

        group.MapPost("/message", (SendMessageRequest request, MillService mill) =>
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
    }
}
