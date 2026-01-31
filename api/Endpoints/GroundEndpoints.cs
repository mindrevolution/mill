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

        // Status
        group.MapGet("/status", (MillService mill) =>
        {
            var status = mill.GetGroundStatus();
            return Results.Ok(status);
        })
        .WithName("GetGroundStatus")
        .WithSummary("Get ground workspace status (isEmpty, hasKickstart)");

        // Templates
        group.MapGet("/archetypes", (MillService mill) =>
        {
            var archetypes = mill.GetArchetypes();
            return Results.Ok(archetypes);
        })
        .WithName("GetArchetypes")
        .WithSummary("Get available product archetypes");

        group.MapGet("/stacks", (MillService mill) =>
        {
            var stacks = mill.GetStacks();
            return Results.Ok(stacks);
        })
        .WithName("GetStacks")
        .WithSummary("Get available tech stacks");

        group.MapGet("/templates/{type}/{id}", (string type, string id, MillService mill) =>
        {
            var validTypes = new[] { "archetypes", "stacks" };
            if (!validTypes.Contains(type.ToLowerInvariant()))
                return Results.BadRequest($"Invalid template type. Must be one of: {string.Join(", ", validTypes)}");

            var template = mill.GetTemplate(type.ToLowerInvariant(), id);
            if (template == null)
                return Results.NotFound();

            return Results.Ok(template);
        })
        .WithName("GetTemplate")
        .WithSummary("Get a specific template's content");

        // Ground items (by category)
        group.MapGet("/{category}", async (string category, MillService mill) =>
        {
            var validCategories = new[] { "personas", "standards", "concepts", "design" };
            if (!validCategories.Contains(category.ToLowerInvariant()))
                return Results.BadRequest($"Invalid category. Must be one of: {string.Join(", ", validCategories)}");

            var items = await mill.GetGroundItems(category.ToLowerInvariant());
            return Results.Ok(items);
        })
        .WithName("GetGroundItems")
        .WithSummary("Get all items in a ground category");

        group.MapGet("/{category}/{id}", async (string category, string id, MillService mill) =>
        {
            var content = await mill.GetGroundItemContent(category.ToLowerInvariant(), id);
            if (content == null)
                return Results.NotFound();

            return Results.Ok(new KnowledgeItemContent(id, category, content));
        })
        .WithName("GetGroundItem")
        .WithSummary("Get a specific ground item's content");

        // Kickstart
        group.MapPost("/kickstart", async (KickstartRequest request, MillService mill, ILlmProvider llmProvider) =>
        {
            try
            {
                var response = await mill.RunKickstart(request, llmProvider);
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
    }
}
