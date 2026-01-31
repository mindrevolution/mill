using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class BriefEndpoints
{
    public static void MapBriefEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/brief")
            .WithTags("Brief");

        group.MapGet("/", async (MillService mill) =>
        {
            var briefs = await mill.GetBriefs();
            return Results.Ok(briefs);
        })
        .WithName("GetBriefs")
        .WithSummary("List all active briefs");

        group.MapGet("/{id}", async (string id, MillService mill) =>
        {
            var brief = await mill.GetBrief(id);
            if (brief == null)
                return Results.NotFound();
            return Results.Ok(brief);
        })
        .WithName("GetBrief")
        .WithSummary("Get a brief with content");

        group.MapPost("/", async (CreateBriefRequest request, MillService mill) =>
        {
            var brief = await mill.CreateBrief(request.Title, request.Intent, request.Type);
            return Results.Created($"/api/brief/{brief.Id}", brief);
        })
        .WithName("CreateBrief")
        .WithSummary("Create a new brief");

        group.MapPut("/{id}", async (string id, UpdateBriefRequest request, MillService mill) =>
        {
            var brief = await mill.UpdateBrief(id, request);
            if (brief == null)
                return Results.NotFound();
            return Results.Ok(brief);
        })
        .WithName("UpdateBrief")
        .WithSummary("Update a brief");

        group.MapPost("/{id}/drop", async (string id, DropBriefRequest request, MillService mill) =>
        {
            var dropped = await mill.DropBrief(id, request.Essence);
            if (dropped == null)
                return Results.NotFound();
            return Results.Ok(dropped);
        })
        .WithName("DropBrief")
        .WithSummary("Drop a brief with essence");

        group.MapPost("/{id}/promote", async (string id, PromoteBriefRequest request, MillService mill) =>
        {
            var draft = await mill.PromoteBrief(id, request.Type);
            if (draft == null)
                return Results.NotFound();
            return Results.Ok(draft);
        })
        .WithName("PromoteBrief")
        .WithSummary("Promote a brief to a draft in Shape");

        group.MapGet("/dropped", async (MillService mill) =>
        {
            var dropped = await mill.GetDroppedBriefs();
            return Results.Ok(dropped);
        })
        .WithName("GetDroppedBriefs")
        .WithSummary("List all dropped briefs");
    }
}
