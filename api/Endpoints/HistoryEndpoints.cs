using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/history")
            .WithTags("History");

        group.MapGet("/", async (MillService mill) =>
        {
            var history = await mill.GetHistory();
            return Results.Json(history, ApiJsonContext.Default.ListHistoryEntry);
        })
        .WithName("GetHistory")
        .WithSummary("Get shipping history");
    }
}
