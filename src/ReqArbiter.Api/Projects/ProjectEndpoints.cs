using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Projects;

namespace ReqArbiter.Api.Projects;

public record CreateProjectRequest(string? Name);

public record UpdateProjectRequest(string? Name, string? SystemName, string? Language, string? Glossary);

public static class ProjectEndpoints
{
    // Registers the /projects routes; called from Program.cs.
    public static void MapProjectEndpoints(this WebApplication app)
    {
        app.MapGet("/projects", async (AppDbContext db) =>
            await db.Projects
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync());

        // Only the name is asked when creating; the catalog settings start with
        // defaults ("el sistema <name>", Spanish, no glossary) and are edited later.
        app.MapPost("/projects", async (CreateProjectRequest request, AppDbContext db) =>
        {
            var name = request.Name?.Trim();
            if (ProjectSettings.ValidateName(name) is { } error)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = [error] });
            }

            var project = new Project
            {
                Name = name!,
                SystemName = ProjectSettings.DefaultSystemName(name!),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            return Results.Created($"/projects/{project.Id}", project);
        });

        // Edits the name and the catalog settings of a project.
        app.MapPut("/projects/{projectId:int}", async (int projectId, UpdateProjectRequest request, AppDbContext db) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null)
            {
                return Results.NotFound();
            }

            var name = request.Name?.Trim();
            var systemName = request.SystemName?.Trim();
            var language = request.Language?.Trim();
            var glossary = string.IsNullOrWhiteSpace(request.Glossary) ? null : request.Glossary.Trim();

            var errors = ProjectSettings.Validate(name, systemName, language, glossary);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            project.Name = name!;
            project.SystemName = systemName!;
            project.Language = language!;
            project.Glossary = glossary;
            await db.SaveChangesAsync();

            return Results.Ok(project);
        });
    }
}
