using MillApi.Models;
using MillApi.Services;
using MillApi.Services.Providers;

namespace MillApi.Endpoints;

public static class GroundEndpoints
{
    public static void MapGroundEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ground")
            .WithTags("Ground");

        group.MapGet("/status", (GroundService ground) =>
        {
            var status = ground.GetStatus();
            return Results.Ok(status);
        })
        .WithName("GetGroundStatus")
        .WithSummary("Get ground workspace status (isEmpty, hasKickstart)");

        group.MapGet("/archetypes", (GroundService ground) =>
        {
            var archetypes = ground.GetArchetypes();
            return Results.Ok(archetypes);
        })
        .WithName("GetArchetypes")
        .WithSummary("Get available product archetypes");

        group.MapGet("/stacks", (GroundService ground) =>
        {
            var stacks = ground.GetStacks();
            return Results.Ok(stacks);
        })
        .WithName("GetStacks")
        .WithSummary("Get available tech stacks");

        group.MapGet("/templates/{type}/{id}", (string type, string id, GroundService ground) =>
        {
            var validTypes = new[] { "archetypes", "stacks" };
            if (!validTypes.Contains(type.ToLowerInvariant()))
                return Results.BadRequest($"Invalid template type. Must be one of: {string.Join(", ", validTypes)}");

            var template = ground.GetTemplate(type.ToLowerInvariant(), id);
            if (template == null)
                return Results.NotFound();

            return Results.Ok(template);
        })
        .WithName("GetTemplate")
        .WithSummary("Get a specific template's content");

        group.MapGet("/{category}", async (string category, GroundService ground) =>
        {
            var validCategories = new[] { "personas", "standards", "concepts", "design" };
            if (!validCategories.Contains(category.ToLowerInvariant()))
                return Results.BadRequest($"Invalid category. Must be one of: {string.Join(", ", validCategories)}");

            var items = await ground.GetItems(category.ToLowerInvariant());
            return Results.Ok(items);
        })
        .WithName("GetGroundItems")
        .WithSummary("Get all items in a ground category");

        group.MapGet("/{category}/{id}", async (string category, string id, GroundService ground) =>
        {
            var content = await ground.GetItemContent(category.ToLowerInvariant(), id);
            if (content == null)
                return Results.NotFound();

            return Results.Ok(new KnowledgeItemContent(id, category, content));
        })
        .WithName("GetGroundItem")
        .WithSummary("Get a specific ground item's content");

        group.MapPost("/kickstart", async (KickstartRequest request, GroundService ground, ILlmProvider llmProvider) =>
        {
            try
            {
                var response = await ground.RunKickstart(request, llmProvider);
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        })
        .WithName("RunKickstart")
        .WithSummary("Run kickstart to generate initial ground files");

        // ─────────────────────────────────────────────────────────────────
        // Observations
        // ─────────────────────────────────────────────────────────────────

        group.MapGet("/observations", async (ObservationService observations) =>
        {
            var list = await observations.GetAll();
            return Results.Ok(list);
        })
        .WithName("GetObservations")
        .WithSummary("Get all pending observations");

        group.MapPost("/observations/{id}/accept", async (string id, ObservationService observations) =>
        {
            var item = await observations.Accept(id);
            if (item == null)
                return Results.NotFound();

            return Results.Ok(item);
        })
        .WithName("AcceptObservation")
        .WithSummary("Accept an observation, promoting it to a ground item");

        group.MapDelete("/observations/{id}", async (string id, ObservationService observations) =>
        {
            await observations.Delete(id);
            return Results.NoContent();
        })
        .WithName("DismissObservation")
        .WithSummary("Dismiss an observation without adding to ground");

        group.MapPost("/observations/{id}/ban", async (string id, ObservationService observations) =>
        {
            await observations.Ban(id);
            return Results.NoContent();
        })
        .WithName("BanObservation")
        .WithSummary("Ban an observation and never suggest it again");
    }
}
