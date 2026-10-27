using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Core.Phases;
using Concilia.Core.Projects;

namespace Concilia.Api.Questions;

// When the validated catalog can be updated with some iterations (step 113,
// decision of the author of 26-10-2026): besides what any catalog needs (no
// iteration open, a finished round, no pending question), each iteration
// chosen must be covered by a round of questions finished after its
// documentation was closed, centred on it or with all of them.
public static class CatalogUpdateInput
{
    public static async Task<string?> CoverageErrorAsync(AppDbContext db, FileStorage storage, int projectId, IReadOnlyList<int> iterations)
    {
        var closed = await db.Iterations.Where(i => i.ProjectId == projectId).ToDictionaryAsync(i => i.Number, i => i.ClosedAt);
        var rounds = (await db.Jobs.Where(j => j.ProjectId == projectId && j.Kind == "questions").ToListAsync())
            .Where(j => HasResult(storage.JobFolder(j.Id)))
            .ToList();
        foreach (var n in iterations)
        {
            if (!closed.TryGetValue(n, out var closedAt) || closedAt is null)
                continue; // the focus rules say it (it does not exist or it is open)
            if (!rounds.Any(r => r.CreatedAt > closedAt && (r.Focus is null || r.Focus.Contains(n))))
                return $"Haz una ronda de preguntas sobre la iteración {n} (centrada en ella o con todas) después de cerrar su documentación.";
        }
        return null;
    }

    // A round counts as finished when its job left a result.
    private static bool HasResult(string jobFolder)
    {
        var path = Path.Combine(jobFolder, PhaseJobResult.FileName);
        return File.Exists(path) && JsonNode.Parse(File.ReadAllText(path))?["fuente"] is JsonValue;
    }
}
