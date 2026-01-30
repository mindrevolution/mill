using Mill.Api.Services;

namespace Mill.Api.Endpoints;

public static class LibraryEndpoints
{
    public static void MapLibraryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/library")
            .WithTags("Library");

        group.MapGet("/{category}", async (string category, MillService mill) =>
        {
            var validCategories = new[] { "personas", "standards", "concepts", "design" };
            if (!validCategories.Contains(category.ToLowerInvariant()))
                return Results.BadRequest($"Invalid category. Must be one of: {string.Join(", ", validCategories)}");

            var items = await mill.GetLibraryItems(category.ToLowerInvariant());
            return Results.Ok(items);
        })
        .WithName("GetLibraryItems")
        .WithSummary("Get all items in a library category");

        group.MapGet("/{category}/{id}", async (string category, string id, MillService mill) =>
        {
            var content = await mill.GetLibraryItemContent(category.ToLowerInvariant(), id);
            if (content == null)
                return Results.NotFound();

            return Results.Ok(new { id, category, content });
        })
        .WithName("GetLibraryItem")
        .WithSummary("Get a specific library item's content");
    }
}
