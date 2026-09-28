using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Projects;

namespace ReqArbiter.Api.Projects;

public record CreateProjectRequest(string? Name);

public static class ProjectEndpoints
{
    public const int MaxNameLength = 200;

    // Registers the /projects routes; called from Program.cs.
    public static void MapProjectEndpoints(this WebApplication app)
    {
        app.MapGet("/projects", async (AppDbContext db) =>
            await db.Projects
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync());

        app.MapPost("/projects", async (CreateProjectRequest request, AppDbContext db) =>
        {
            var name = request.Name?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["El nombre es obligatorio."],
                });
            }

            if (name.Length > MaxNameLength)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = [$"El nombre no puede superar {MaxNameLength} caracteres."],
                });
            }

            var project = new Project { Name = name, CreatedAt = DateTimeOffset.UtcNow };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            return Results.Created($"/projects/{project.Id}", project);
        });
    }
}
