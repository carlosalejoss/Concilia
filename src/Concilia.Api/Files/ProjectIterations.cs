using Microsoft.EntityFrameworkCore;
using Concilia.Api.Persistence;
using Concilia.Core.Deliverables;
using Concilia.Core.Phases;
using Concilia.Core.Projects;

namespace Concilia.Api.Files;

// The iteration of each element and the coverage of each deliverable
// (milestone 22, step 111), read from the database: which iteration each
// document is from, with the numbering of PhaseJobs.LoadDocumentsAsync.
public static class ProjectIterations
{
    // "DOC-01" → 1: the documents with text of the project, numbered by upload
    // order in the whole project (as the phases receive them), with the number
    // of their iteration.
    public static async Task<Dictionary<string, int>> DocumentsAsync(AppDbContext db, int projectId)
    {
        var numbers = await db.Iterations.Where(i => i.ProjectId == projectId).ToDictionaryAsync(i => i.Id, i => i.Number);
        var files = await db.ProjectFiles
            .Where(f => f.ProjectId == projectId && f.TextStoredName != null)
            .OrderBy(f => f.UploadedAt).ThenBy(f => f.Id)
            .Select(f => f.IterationId)
            .ToListAsync();
        var documents = new Dictionary<string, int>();
        for (var i = 0; i < files.Count; i++)
        {
            if (files[i] is { } id && numbers.TryGetValue(id, out var number))
                documents[$"DOC-{i + 1:00}"] = number;
        }
        return documents;
    }

    // The iterations of the documents, questions, requirements and stories of a project.
    public static async Task<ElementIterations> ElementsAsync(AppDbContext db, int projectId)
    {
        var questions = await db.Questions.Where(q => q.ProjectId == projectId)
            .Select(q => new { q.Code, q.Sources })
            .ToListAsync();
        return new ElementIterations(await DocumentsAsync(db, projectId),
            questions.ToDictionary(q => q.Code, q => (IReadOnlyList<string>)q.Sources));
    }

    // The iterations whose documentation a catalog or backlog job read: those
    // of the documents of its plan.
    public static IReadOnlyList<int> OfPlan(PhasePlan plan, IReadOnlyDictionary<string, int> documents) =>
        plan.Fragments.Select(f => f.Source).Distinct()
            .Select(doc => documents.TryGetValue(doc, out var number) ? number : (int?)null)
            .OfType<int>().Distinct().Order().ToList();

    // The closed iterations with documents that a deliverable does not include
    // (what the web says it lacks). The open one is left out: nothing can be
    // generated with it until it is closed.
    public static async Task<IReadOnlyList<int>> MissingAsync(AppDbContext db, Deliverable d)
    {
        if (d.Iterations is null)
            return [];
        var closed = await db.Iterations
            .Where(i => i.ProjectId == d.ProjectId && i.Status == IterationStatus.Closed && db.ProjectFiles.Any(f => f.IterationId == i.Id && f.TextStoredName != null))
            .Select(i => i.Number)
            .ToListAsync();
        return closed.Except(d.Iterations).Order().ToList();
    }

    // At start-up: the deliverables and versions from before the coverage get
    // theirs. A catalog or backlog, from the plan of its job; a mockup (or a
    // job without its folder), the iterations of the documents uploaded
    // before its job was created. Only does something the first time.
    public static async Task RecordExistingCoverageAsync(AppDbContext db, FileStorage storage)
    {
        var deliverables = await db.Deliverables.Where(d => d.Iterations == null).ToListAsync();
        var versions = await db.DeliverableVersions.Where(v => v.Iterations == null).ToListAsync();
        var coverage = new Dictionary<Guid, List<int>>();
        async Task<List<int>> Of(int projectId, Guid jobId)
        {
            if (!coverage.TryGetValue(jobId, out var numbers))
            {
                numbers = [.. await JobCoverageAsync(db, storage, projectId, jobId)];
                coverage[jobId] = numbers;
            }
            return numbers;
        }
        foreach (var d in deliverables)
            d.Iterations = [.. await Of(d.ProjectId, d.JobId)];
        foreach (var v in versions)
            v.Iterations = [.. await Of(v.ProjectId, v.JobId)];
        await db.SaveChangesAsync();
    }

    private static async Task<IReadOnlyList<int>> JobCoverageAsync(AppDbContext db, FileStorage storage, int projectId, Guid jobId)
    {
        var documents = await DocumentsAsync(db, projectId);
        if (File.Exists(Path.Combine(storage.JobFolder(jobId), PhasePlan.FileName)))
            return OfPlan(PhasePlan.Load(storage.JobFolder(jobId)), documents);
        var job = await db.Jobs.FindAsync(jobId);
        var before = job?.CreatedAt ?? DateTimeOffset.MaxValue;
        var iterations = await db.ProjectFiles
            .Where(f => f.ProjectId == projectId && f.TextStoredName != null && f.UploadedAt <= before && f.IterationId != null)
            .Select(f => f.IterationId!.Value)
            .Distinct()
            .ToListAsync();
        return await db.Iterations.Where(i => iterations.Contains(i.Id)).Select(i => i.Number).Distinct().OrderBy(n => n).ToListAsync();
    }
}
