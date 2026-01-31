using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

/// <summary>
/// Legacy endpoints for backward compatibility. Use /api/ground instead.
/// </summary>
public static class KnowledgeEndpoints
{
    public static void MapKnowledgeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/knowledge")
            .WithTags("Knowledge");

        group.MapGet("/{category}", async (string category, GroundService ground) =>
        {
            var validCategories = new[] { "personas", "standards", "concepts", "design" };
            if (!validCategories.Contains(category.ToLowerInvariant()))
                return Results.BadRequest($"Invalid category. Must be one of: {string.Join(", ", validCategories)}");

            var items = await ground.GetItems(category.ToLowerInvariant());
            return Results.Ok(items);
        })
        .WithName("GetKnowledgeItems")
        .WithSummary("Get all items in a library category");

        group.MapGet("/{category}/{id}", async (string category, string id, GroundService ground) =>
        {
            var content = await ground.GetItemContent(category.ToLowerInvariant(), id);
            if (content == null)
                return Results.NotFound();

            return Results.Ok(new KnowledgeItemContent(id, category, content));
        })
        .WithName("GetKnowledgeItem")
        .WithSummary("Get a specific library item's content");
    }
}
