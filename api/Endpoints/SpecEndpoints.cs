using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class SpecEndpoints
{
    public static void MapSpecEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/spec")
            .WithTags("Spec");

        group.MapGet("/drafts", async (MillService mill) =>
        {
            var drafts = await mill.GetDrafts();
            return Results.Json(drafts, ApiJsonContext.Default.ListDraft);
        })
        .WithName("GetDrafts")
        .WithSummary("List all spec drafts");

        group.MapGet("/drafts/{id}", async (string id, MillService mill) =>
        {
            var draft = await mill.GetDraftDetail(id);
            return draft != null
                ? Results.Json(draft, ApiJsonContext.Default.DraftDetail)
                : Results.NotFound();
        })
        .WithName("GetDraftDetail")
        .WithSummary("Get single draft with full content");

        group.MapGet("/issues", async (MillService mill, bool refresh = false) =>
        {
            var issues = await mill.GetIssues(forceRefresh: refresh);
            return Results.Json(issues, ApiJsonContext.Default.ListIssue);
        })
        .WithName("GetIssues")
        .WithSummary("List GitHub issues (specs). Use ?refresh=true to bypass cache.");

        group.MapGet("/issues/{number:int}", async (int number, MillService mill, bool refresh = false) =>
        {
            var issue = await mill.GetIssueDetail(number, forceRefresh: refresh);
            return issue != null
                ? Results.Json(issue, ApiJsonContext.Default.IssueDetail)
                : Results.Json(new IssueNotFoundError($"Issue #{number} not found"), ApiJsonContext.Default.IssueNotFoundError);
        })
        .WithName("GetIssueDetail")
        .WithSummary("Get single GitHub issue with full details. Use ?refresh=true to bypass cache.");

        group.MapPost("/start", (StartSpecRequest request, MillService mill) =>
        {
            // TODO: Start interactive spec session
            // This would spawn `mill spec --interactive` and manage stdin/stdout
            var sessionId = Guid.NewGuid().ToString("N")[..8];

            return Results.Json(new SpecSession(
                Id: sessionId,
                DraftId: request.DraftId,
                StartedAt: DateTime.UtcNow
            ), ApiJsonContext.Default.SpecSession);
        })
        .WithName("StartSpec")
        .WithSummary("Start a new spec creation session");

        group.MapPost("/message", (SendMessageRequest request, MillService mill) =>
        {
            // TODO: Send message to active spec session
            // For now, return a placeholder response
            return Results.Json(new SendMessageResponse(
                Content: "I understand you want to build something. Let me help you create a spec. What problem are you trying to solve?",
                UpdatedDraft: null
            ), ApiJsonContext.Default.SendMessageResponse);
        })
        .WithName("SendSpecMessage")
        .WithSummary("Send a message in an active spec session");
    }
}
