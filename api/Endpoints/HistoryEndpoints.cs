using MillApi.Services;

namespace MillApi.Endpoints;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/history")
            .WithTags("History");

        group.MapGet("/", async (ShipService ship) =>
        {
            var history = await ship.GetHistory();
            return Results.Ok(history);
        })
        .WithName("GetHistory")
        .WithSummary("Get shipping history");
    }
}
