using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Core.Files;
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

        // The extracted plain text of one file, or 404 if it has none.
        app.MapGet("/projects/{projectId:int}/files/{fileId:int}/text",
            async (int projectId, int fileId, AppDbContext db, FileStorage storage) =>
        {
            var file = await db.ProjectFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectId == projectId);

            if (file?.TextStoredName is null)
            {
                return Results.NotFound();
            }

            var path = Path.Combine(storage.ProjectFolder(projectId), file.TextStoredName);
            return Results.Text(await File.ReadAllTextAsync(path), "text/plain; charset=utf-8");
        });

        // Expects a multipart/form-data request with the file in a field named "file".
        app.MapPost("/projects/{projectId:int}/files",
            async (int projectId, IFormFile? file, AppDbContext db, FileStorage storage) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }

            if (file is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Selecciona un archivo."],
                });
            }

            // Only the last part of the name the browser sends ("C:\x\a.md" -> "a.md").
            var fileName = Path.GetFileName(file.FileName);

            // Type and size rules (UploadRules, in Core).
            var error = UploadRules.Validate(fileName, file.Length);
            if (error is not null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = [error],
                });
            }

            // Read the whole file into memory (at most UploadRules.MaxBytes), so we
            // can hash it and extract its text before writing anything to disk.
            byte[] content;
            using (var buffer = new MemoryStream())
            {
                await file.CopyToAsync(buffer);
                content = buffer.ToArray();
            }
            var sha256 = Convert.ToHexStringLower(SHA256.HashData(content));

            // A damaged PDF or DOCX is rejected here, and nothing is stored.
            string? text;
            try
            {
                text = TextExtractor.Extract(content, Path.GetExtension(fileName));
            }
            catch (TextExtractionException e)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = [e.Message],
                });
            }

            // The name on disk is generated; only the extension is kept from the user.
            var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(fileName).ToLowerInvariant();

            var folder = storage.ProjectFolder(projectId);
            Directory.CreateDirectory(folder);
            await File.WriteAllBytesAsync(Path.Combine(folder, storedName), content);

            // The plain text goes next to the original ("{guid}.extracted.txt"),
            // always as UTF-8.
            string? textStoredName = null;
            if (text is not null)
            {
                textStoredName = Path.GetFileNameWithoutExtension(storedName) + ".extracted.txt";
                await File.WriteAllTextAsync(Path.Combine(folder, textStoredName), text);
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
                TextStoredName = textStoredName,
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
