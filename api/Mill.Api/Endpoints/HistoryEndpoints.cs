using Mill.Api.Services;

namespace Mill.Api.Endpoints;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/history")
            .WithTags("History");

        group.MapGet("/", async (MillService mill) =>
        {
            var history = await mill.GetHistory();
            return Results.Ok(history);
        })
        .WithName("GetHistory")
        .WithSummary("Get shipping history");
    }
}
