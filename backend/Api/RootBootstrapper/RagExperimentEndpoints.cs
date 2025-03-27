using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace Horus.RootBootstrapper;

public static class RagExperimentEndpoints
{
    public static void MapRagExperimentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // GET: /api/v1/rag-experiments
        endpoints.MapGet("/api/v1/rag-experiments", async (HorusContext db) =>
            {
                var results = await db.RagExperiments
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
                return Results.Ok(results);
            })
            .WithName("GetAllRagExperiments");

        // POST: /api/v1/rag-experiments
        endpoints.MapPost("/api/v1/rag-experiments", async (HorusContext db, RagExperiment experiment) =>
            {
                experiment.CreatedAt = DateTime.UtcNow; // If not relying on default in DB
                db.RagExperiments.Add(experiment);
                await db.SaveChangesAsync();
                return Results.Created($"/api/v1/rag-experiments/{experiment.Id}", experiment);
            })
            .WithName("CreateRagExperiment");
    }
}