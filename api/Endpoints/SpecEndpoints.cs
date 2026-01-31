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
    }
}
