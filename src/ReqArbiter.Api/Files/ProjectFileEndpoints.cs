using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Projects;

namespace ReqArbiter.Api.Files;

public static class ProjectFileEndpoints
{
    // Registers the /projects/{projectId}/files routes; called from Program.cs.
    public static void MapProjectFileEndpoints(this WebApplication app)
    {
        app.MapGet("/projects/{projectId:int}/files", async (int projectId, AppDbContext db) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }

            var files = await db.ProjectFiles
                .Where(f => f.ProjectId == projectId)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();

            return Results.Ok(files);
        });

        // Expects a multipart/form-data request with the file in a field named "file".
        app.MapPost("/projects/{projectId:int}/files",
            async (int projectId, IFormFile? file, AppDbContext db, FileStorage storage) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }

            if (file is null || file.Length == 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Selecciona un archivo que no esté vacío."],
                });
            }

            // Only the last part of the name the browser sends ("C:\x\a.md" -> "a.md").
            var fileName = Path.GetFileName(file.FileName);

            // The name on disk is generated; only the extension is kept from the user.
            var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName).ToLowerInvariant();

            var folder = storage.ProjectFolder(projectId);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, storedName);

            await using (var output = File.Create(path))
            {
                await file.CopyToAsync(output);
            }

            // Hash the saved copy, so the hash describes exactly what is on disk.
            string sha256;
            await using (var saved = File.OpenRead(path))
            {
                sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(saved));
            }

            var record = new ProjectFile
            {
                ProjectId = projectId,
                FileName = fileName,
                StoredName = storedName,
                ContentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType,
                SizeBytes = file.Length,
                Sha256 = sha256,
                UploadedAt = DateTimeOffset.UtcNow,
            };
            db.ProjectFiles.Add(record);
            await db.SaveChangesAsync();

            return Results.Created($"/projects/{projectId}/files/{record.Id}", record);
        })
        // ASP.NET Core requires an anti-forgery token for form uploads by default.
        // That protects cookie-authenticated sites; this API has no login or
        // cookies (see CLAUDE.md), so the check is turned off for this endpoint.
        .DisableAntiforgery();
    }
}
