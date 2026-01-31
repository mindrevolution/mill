using MillApi.Models;
using MillApi.Services;

namespace MillApi.Endpoints;

public static class BriefEndpoints
{
    public static void MapBriefEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/brief")
            .WithTags("Brief");

        group.MapGet("/", async (BriefService briefs) =>
        {
            var list = await briefs.GetAll();
            return Results.Ok(list);
        })
        .WithName("GetBriefs")
        .WithSummary("List all active briefs");

        group.MapGet("/{id}", async (string id, BriefService briefs) =>
        {
            var brief = await briefs.Get(id);
            if (brief == null)
                return Results.NotFound();
            return Results.Ok(brief);
        })
        .WithName("GetBrief")
        .WithSummary("Get a brief with content");

        group.MapPost("/", async (CreateBriefRequest request, BriefService briefs) =>
        {
            var brief = await briefs.Create(request.Title, request.Intent, request.Type);
            return Results.Created($"/api/brief/{brief.Id}", brief);
        })
        .WithName("CreateBrief")
        .WithSummary("Create a new brief");

        group.MapPut("/{id}", async (string id, UpdateBriefRequest request, BriefService briefs) =>
        {
            var brief = await briefs.Update(id, request);
            if (brief == null)
                return Results.NotFound();
            return Results.Ok(brief);
        })
        .WithName("UpdateBrief")
        .WithSummary("Update a brief");

        group.MapPost("/{id}/drop", async (string id, DropBriefRequest request, BriefService briefs) =>
        {
            var dropped = await briefs.Drop(id, request.Essence);
            if (dropped == null)
                return Results.NotFound();
            return Results.Ok(dropped);
        })
        .WithName("DropBrief")
        .WithSummary("Drop a brief with essence");

        group.MapPost("/{id}/promote", async (string id, PromoteBriefRequest request, BriefService briefs) =>
        {
            var draft = await briefs.Promote(id, request.Type);
            if (draft == null)
                return Results.NotFound();
            return Results.Ok(draft);
        })
        .WithName("PromoteBrief")
        .WithSummary("Promote a brief to a draft in Shape");

        group.MapGet("/dropped", async (BriefService briefs) =>
        {
            var dropped = await briefs.GetDropped();
            return Results.Ok(dropped);
        })
        .WithName("GetDroppedBriefs")
        .WithSummary("List all dropped briefs");
    }
}
