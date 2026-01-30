using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class KnowledgeEndpoints
{
    public static void MapKnowledgeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/knowledge")
            .WithTags("Knowledge");

        group.MapGet("/{category}", async (string category, MillService mill) =>
        {
            var validCategories = new[] { "personas", "standards", "concepts", "design" };
            if (!validCategories.Contains(category.ToLowerInvariant()))
                return Results.BadRequest($"Invalid category. Must be one of: {string.Join(", ", validCategories)}");

            var items = await mill.GetKnowledgeItems(category.ToLowerInvariant());
            return Results.Json(items, ApiJsonContext.Default.ListKnowledgeItem);
        })
        .WithName("GetKnowledgeItems")
        .WithSummary("Get all items in a library category");

        group.MapGet("/{category}/{id}", async (string category, string id, MillService mill) =>
        {
            var content = await mill.GetKnowledgeItemContent(category.ToLowerInvariant(), id);
            if (content == null)
                return Results.NotFound();

            return Results.Json(new KnowledgeItemContent(id, category, content), ApiJsonContext.Default.KnowledgeItemContent);
        })
        .WithName("GetKnowledgeItem")
        .WithSummary("Get a specific library item's content");
    }
}
