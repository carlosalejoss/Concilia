using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Api.Projects;

public record CreateProjectRequest(string? Name);

public record UpdateProjectRequest(string? Name, string? SystemName, string? Language, string? Glossary);

// Confirmation: the name of the project, typed by the user.
public record DeleteProjectRequest(string? Confirmation);

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

        // Deletes a project with everything it has: files, questions, jobs,
        // runs, deliverables (the database removes them in cascade) and their
        // folders on disk. The body must carry the name of the project, typed
        // by the user; a project with a job in progress is not deleted.
        app.MapDelete("/projects/{projectId:int}",
            async (int projectId, [FromBody] DeleteProjectRequest request, AppDbContext db, FileStorage storage) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null)
            {
                return Results.NotFound();
            }
            if (!ProjectSettings.ConfirmsDeletion(project.Name, request.Confirmation))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["confirmation"] = ["Para eliminar el proyecto, escribe su nombre exactamente como aparece."],
                });
            }
            if (await db.Runs.AnyAsync(r => r.ProjectId == projectId && (r.State == RunState.Queued || r.State == RunState.Running)))
            {
                // 409, with the same shape as the other errors so the web shows the message.
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["project"] = ["El proyecto tiene un trabajo en curso. Espera a que termine para eliminarlo."] },
                    statusCode: StatusCodes.Status409Conflict);
            }

            var jobIds = await db.Jobs.Where(j => j.ProjectId == projectId).Select(j => j.Id).ToListAsync();
            var runIds = await db.Runs.Where(r => r.ProjectId == projectId).Select(r => r.Id).ToListAsync();
            db.Projects.Remove(project);
            await db.SaveChangesAsync();
            // After the database: if deleting a folder fails, the project is
            // already gone and only some disk space is left behind.
            storage.DeleteProjectData(projectId, jobIds, runIds);
            return Results.NoContent();
        });
    }
}
