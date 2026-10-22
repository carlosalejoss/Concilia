using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Core.Catalog;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using Concilia.Core.Projects;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Api.Runs;

// A new job of a v0 phase, ready to be stored and queued; or why it cannot be launched.
public record NewPhaseJob(Job? Job, IReadOnlyList<Run> Runs, PhasePlan? Plan, string? Error);

// Creates the jobs of the v0 phases: the plan (all the fragments, the input of
// the phase and the random order of the proposals) and three runs: A, B and
// the arbiter (ARB), which waits for A and B. Kept apart from the endpoints so
// it can be tested without a database.
public static class PhaseJobs
{
    // The project documents with extracted text, numbered DOC-01, DOC-02... in upload order.
    public static async Task<List<CatalogDocument>> LoadDocumentsAsync(AppDbContext db, FileStorage storage, int projectId)
    {
        var files = await db.ProjectFiles
            .Where(f => f.ProjectId == projectId && f.TextStoredName != null)
            .OrderBy(f => f.UploadedAt).ThenBy(f => f.Id)
            .ToListAsync();
        var documents = new List<CatalogDocument>();
        for (var i = 0; i < files.Count; i++)
        {
            var text = await File.ReadAllTextAsync(Path.Combine(storage.ProjectFolder(projectId), files[i].TextStoredName!));
            documents.Add(new CatalogDocument($"DOC-{i + 1:00}", files[i].FileName, text));
        }
        return documents;
    }

    // input: the parts of the phase input that depend on the earlier phases
    // (previous questions, answers, validated catalog); the documentation and
    // the project settings are filled here. The prompt of A and B must fit in
    // the input budget, and the documentation (read by parts from input/) must
    // not pass RunnerOptions.MaxDocumentationTokens: if not, the job is not
    // created.
    public static NewPhaseJob Create(
        Project project, IReadOnlyList<CatalogDocument> documents, PhaseKind kind, PhaseInput input,
        IReadOnlyList<string> answeredQuestionIds, RunnerOptions options, string providerName, Random random,
        IReadOnlyList<string>? pendingQuestionIds = null)
    {
        if (documents.Count == 0)
            return new NewPhaseJob(null, [], null, "El proyecto no tiene archivos con texto. Sube algún documento con texto.");

        var fragments = documents.SelectMany(d => Fragmenter.FragmentDocument(d.Id, d.Text)).ToList();
        var fullInput = input with
        {
            SystemName = project.SystemName,
            Language = project.Language,
            Glossary = project.Glossary ?? "",
            Documentation = DocumentationFile.Build(fragments, documents.ToDictionary(d => d.Id, d => d.Name)),
        };
        var documentationTokens = InputBudget.EstimateTokens(fullInput.Documentation);
        if (documentationTokens > options.MaxDocumentationTokens)
            return new NewPhaseJob(null, [], null,
                $"La documentación del proyecto es demasiado grande: unos {documentationTokens:N0} tokens estimados, y el máximo es " +
                $"{options.MaxDocumentationTokens:N0}. Quita archivos o deja solo las partes que describen el sistema.");
        var prompt = PhasePrompt.Build(PhaseDefinition.For(kind, PhaseRole.Proposal), fullInput);
        var budgetError = InputBudget.Check(prompt, new PhaseOptions().InputTokenBudget);
        if (budgetError is not null)
            return new NewPhaseJob(null, [], null, budgetError);

        var now = DateTimeOffset.UtcNow;
        var job = new Job { Id = Guid.NewGuid(), ProjectId = project.Id, Kind = PhasePlan.JobKind(kind), CreatedAt = now };
        var plan = new PhasePlan(kind, fullInput, fragments, answeredQuestionIds, PhasePlan.RandomOrder(random))
        {
            PendingQuestionIds = pendingQuestionIds ?? [],
        };
        Run NewRun(string slot, string model) => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            JobId = job.Id,
            Slot = slot,
            Provider = providerName,
            Model = model,
            State = RunState.Queued,
            CreatedAt = now,
            PromptVersion = PhaseExecutor.PromptVersion,
        };
        // The suggestions of answers are a single run (SUG), without arbiter.
        IReadOnlyList<Run> runs = kind == PhaseKind.Suggestions
            ? [NewRun("SUG", options.ModelSuggestions)]
            : [NewRun("A", options.ModelA), NewRun("B", options.ModelB), NewRun("ARB", options.ModelArbiter)];
        return new NewPhaseJob(job, runs, plan, null);
    }

    // Stores a new job (its plan, the job and its three runs) and queues it.
    // Answers 202 with the job, or 400 with the reason it cannot be created.
    public static async Task<IResult> EnqueueAsync(AppDbContext db, JobQueue queue, FileStorage storage, NewPhaseJob created)
    {
        if (created.Error is not null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["project"] = [created.Error] });
        }
        created.Plan!.Save(storage.JobFolder(created.Job!.Id));
        db.Jobs.Add(created.Job);
        db.Runs.AddRange(created.Runs);
        await db.SaveChangesAsync();
        queue.Enqueue(created.Job.Id);
        return Results.Accepted($"/jobs/{created.Job.Id}", new JobView(created.Job, created.Runs));
    }

    // data/jobs/{id}/resultado-arbitro.json: the result of the job, frozen.
    // The next steps (questions, catalog, backlog) take it from here.
    public static void WriteResult(string jobFolder, PhaseJobResult result)
    {
        Directory.CreateDirectory(jobFolder);
        var content = new JsonObject
        {
            ["fuente"] = result.Source,
            ["verificacion_cruzada"] = result.CrossChecked,
            ["mensaje"] = result.Message,
            ["salida"] = result.Output is null ? null : JsonNode.Parse(result.Output),
            // Where the arbiter went against the matching of the platform (step 88).
            ["desviaciones"] = new JsonArray(result.Deviations.Select(d => (JsonNode)new JsonObject
            {
                ["id"] = d.ElementId,
                ["tipo"] = d.Kind == DeviationKind.JoinedDifferent ? "junta-distintos" : "separa-iguales",
                ["similitud"] = Math.Round(d.Similarity, 3),
                ["explicacion"] = d.Explanation,
            }).ToArray()),
            // What the matching cost: the embeddings are local, so time and not money (step 95).
            ["emparejamiento"] = result.MatchingTime is { } time
                ? new JsonObject { ["segundos"] = Math.Round(time.Seconds, 2), ["segundos_cpu"] = Math.Round(time.CpuSeconds, 2) }
                : null,
        };
        // Step 100: the matching computed for the arbiter, so nothing computes it again.
        if (result.Similarities is not null)
            MatchingCache.Write(jobFolder, result.Similarities);
        File.WriteAllText(Path.Combine(jobFolder, PhaseJobResult.FileName),
            content.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }
}
