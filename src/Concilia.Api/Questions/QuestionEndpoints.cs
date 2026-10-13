using Microsoft.EntityFrameworkCore;
using Concilia.Api.Files;
using Concilia.Api.Persistence;
using Concilia.Api.Projects;
using Concilia.Api.Runs;
using Concilia.Core.Deliverables;
using Concilia.Core.Flow;
using Concilia.Core.Phases;
using Concilia.Core.Questions;
using Concilia.Core.Runner;

namespace Concilia.Api.Questions;

// FromSuggestion: the user started from the AI's suggestion (to record the origin of the answer).
public record AnswerRequest(string? Answer, bool FromSuggestion = false);
public record DiscardRequest(string? Reason);

public static class QuestionEndpoints
{
    public static void MapQuestionEndpoints(this WebApplication app)
    {
        // A round of questions: A ‖ B → arbiter over all the documentation and
        // the questions already asked (so they are not asked again). When the
        // arbiter ends, JobWorker stores its questions as pending (StoreRoundAsync).
        app.MapPost("/projects/{projectId:int}/question-rounds",
            async (int projectId, AppDbContext db, JobQueue queue, IRunnerProvider provider, RunnerOptions options, FileStorage storage) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null)
            {
                return Results.NotFound();
            }

            var step = ProjectFlow.Compute(await FlowEndpoints.ReadFactsAsync(db, storage, projectId)).Single(s => s.Step == FlowStep.Questions);
            if (!step.CanLaunch)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["project"] = [step.Reason!] });
            }

            var previous = await db.Questions.Where(q => q.ProjectId == projectId).OrderBy(q => q.Code).ToListAsync();
            var input = new PhaseInput
            {
                SystemName = project.SystemName,
                Language = project.Language,
                PreviousQuestions = PhasePrompt.PreviousQuestionsBlock(previous.Select(QuestionRules.ForPrompt)),
            };
            var documents = await PhaseJobs.LoadDocumentsAsync(db, storage, projectId);
            var created = PhaseJobs.Create(project, documents, PhaseKind.Questions, input, [], options, provider.Name, Random.Shared);
            return await PhaseJobs.EnqueueAsync(db, queue, storage, created);
        });

        // The catalog v0: A ‖ B → arbiter over all the documentation and the
        // answered questions. Only when the step is available: at least one
        // finished round and no pending question.
        app.MapPost("/projects/{projectId:int}/catalog-jobs",
            async (int projectId, AppDbContext db, JobQueue queue, IRunnerProvider provider, RunnerOptions options, FileStorage storage) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null)
            {
                return Results.NotFound();
            }

            var step = ProjectFlow.Compute(await FlowEndpoints.ReadFactsAsync(db, storage, projectId)).Single(s => s.Step == FlowStep.Catalog);
            if (!step.CanLaunch)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["project"] = [step.Reason!] });
            }

            var answered = await db.Questions
                .Where(q => q.ProjectId == projectId && q.Status == QuestionStatus.Answered)
                .OrderBy(q => q.Code)
                .ToListAsync();
            var input = new PhaseInput
            {
                SystemName = project.SystemName,
                Language = project.Language,
                Answers = PhasePrompt.AnswersBlock(answered.Select(QuestionRules.ForPrompt)),
            };
            var documents = await PhaseJobs.LoadDocumentsAsync(db, storage, projectId);
            var created = PhaseJobs.Create(project, documents, PhaseKind.Catalog, input, answered.Select(q => q.Code).ToList(), options, provider.Name, Random.Shared);
            return await PhaseJobs.EnqueueAsync(db, queue, storage, created);
        });

        // The backlog v0: A ‖ B → arbiter from the validated catalog (only the
        // fields of its schema) and the answered questions. Only when the
        // catalog is validated.
        app.MapPost("/projects/{projectId:int}/backlog-jobs",
            async (int projectId, AppDbContext db, JobQueue queue, IRunnerProvider provider, RunnerOptions options, FileStorage storage) =>
        {
            var project = await db.Projects.FindAsync(projectId);
            if (project is null)
            {
                return Results.NotFound();
            }

            var step = ProjectFlow.Compute(await FlowEndpoints.ReadFactsAsync(db, storage, projectId)).Single(s => s.Step == FlowStep.Backlog);
            if (!step.CanLaunch)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["project"] = [step.Reason!] });
            }

            var catalog = await Deliverables.DeliverableEndpoints.CurrentAsync(db, projectId, DeliverableKind.Catalog);
            var answered = await db.Questions
                .Where(q => q.ProjectId == projectId && q.Status == QuestionStatus.Answered)
                .OrderBy(q => q.Code)
                .ToListAsync();
            var input = new PhaseInput
            {
                SystemName = project.SystemName,
                Language = project.Language,
                Answers = PhasePrompt.AnswersBlock(answered.Select(QuestionRules.ForPrompt)),
                Catalog = CatalogDraft.ForNextPhase(catalog!.Content),
            };
            var documents = await PhaseJobs.LoadDocumentsAsync(db, storage, projectId);
            var created = PhaseJobs.Create(project, documents, PhaseKind.Backlog, input, answered.Select(q => q.Code).ToList(), options, provider.Name, Random.Shared);
            return await PhaseJobs.EnqueueAsync(db, queue, storage, created);
        });

        // Suggested answers for all the pending questions of a round. Nothing is
        // answered: each question keeps the suggestion for the user to use, edit
        // or ignore (StoreSuggestionsAsync, when the job ends).
        app.MapPost("/question-rounds/{jobId:guid}/suggestions",
            async (Guid jobId, AppDbContext db, JobQueue queue, IRunnerProvider provider, RunnerOptions options, FileStorage storage) =>
        {
            var round = await db.Jobs.FindAsync(jobId);
            if (round is null || round.Kind != PhasePlan.JobKind(PhaseKind.Questions))
            {
                return Results.NotFound();
            }
            var pending = await db.Questions
                .Where(q => q.JobId == jobId && q.Status == QuestionStatus.Pending)
                .OrderBy(q => q.Code)
                .ToListAsync();
            return await SuggestAsync(db, queue, provider, options, storage, round.ProjectId, pending);
        });

        // The suggested answer for one pending question.
        app.MapPost("/questions/{id:int}/suggestion",
            async (int id, AppDbContext db, JobQueue queue, IRunnerProvider provider, RunnerOptions options, FileStorage storage) =>
        {
            var question = await db.Questions.FindAsync(id);
            if (question is null)
            {
                return Results.NotFound();
            }
            var pending = question.Status == QuestionStatus.Pending ? [question] : new List<Question>();
            return await SuggestAsync(db, queue, provider, options, storage, question.ProjectId, pending);
        });

        // All the questions of the project, by code (optionally only one status: ?status=Pending).
        app.MapGet("/projects/{projectId:int}/questions", async (int projectId, QuestionStatus? status, AppDbContext db) =>
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return Results.NotFound();
            }
            var query = db.Questions.Where(q => q.ProjectId == projectId);
            if (status is not null)
            {
                query = query.Where(q => q.Status == status);
            }
            return Results.Ok(await query.OrderBy(q => q.Code).ToListAsync());
        });

        // Answer a question, or change its answer.
        app.MapPut("/questions/{id:int}/answer", (int id, AnswerRequest body, AppDbContext db) =>
            ApplyAsync(db, id, q => QuestionRules.Answer(q, body.Answer, DateTimeOffset.UtcNow, body.FromSuggestion)));

        // Discard a question (not needed, out of scope…), with an optional reason.
        app.MapPost("/questions/{id:int}/discard", (int id, DiscardRequest body, AppDbContext db) =>
            ApplyAsync(db, id, q => QuestionRules.Discard(q, body.Reason, DateTimeOffset.UtcNow)));

        // Back to pending.
        app.MapPost("/questions/{id:int}/reopen", (int id, AppDbContext db) =>
            ApplyAsync(db, id, q => QuestionRules.Reopen(q, DateTimeOffset.UtcNow)));
    }

    // Applies one action of QuestionRules and saves the question. The change it
    // makes to the flow (QuestionChange.Change) outdates the catalog, the
    // backlog and the mockup, if they exist.
    private static async Task<IResult> ApplyAsync(AppDbContext db, int id, Func<Question, QuestionChange> action)
    {
        var question = await db.Questions.FindAsync(id);
        if (question is null)
        {
            return Results.NotFound();
        }
        var change = action(question);
        if (change.Error is not null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["question"] = [change.Error] });
        }
        if (change.Change is { } flowChange)
        {
            await Deliverables.DeliverableEndpoints.ApplyChangeAsync(db, question.ProjectId, flowChange);
        }
        await db.SaveChangesAsync();
        return Results.Ok(question);
    }

    // Called by JobWorker when a suggestions job ends: each pending question
    // keeps its suggested answer (nothing is answered).
    public static async Task StoreSuggestionsAsync(AppDbContext db, int projectId, PhaseJobResult result)
    {
        if (result.Output is null)
        {
            return;
        }
        var questions = await db.Questions.Where(q => q.ProjectId == projectId).ToListAsync();
        QuestionRules.ApplySuggestions(questions, result.Output, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    // A suggestions job: one model, a single run (RUNNER_MODEL_SUGGESTIONS),
    // with all the documentation, the answers so far and the pending questions
    // to answer. One at a time per project.
    private static async Task<IResult> SuggestAsync(AppDbContext db, JobQueue queue, IRunnerProvider provider,
        RunnerOptions options, FileStorage storage, int projectId, List<Question> pending)
    {
        if (pending.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["questions"] = ["No hay preguntas pendientes para sugerir."] });
        }
        if (await SuggestionsRunningAsync(db, projectId))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["questions"] = ["Ya se están generando sugerencias: espera a que terminen."] });
        }

        var project = (await db.Projects.FindAsync(projectId))!;
        var answered = await db.Questions
            .Where(q => q.ProjectId == projectId && q.Status == QuestionStatus.Answered)
            .OrderBy(q => q.Code)
            .ToListAsync();
        var input = new PhaseInput
        {
            SystemName = project.SystemName,
            Language = project.Language,
            Answers = PhasePrompt.AnswersBlock(answered.Select(QuestionRules.ForPrompt)),
            PendingQuestions = PhasePrompt.PendingQuestionsBlock(pending.Select(q => (q.Code, q.Text, q.Reason))),
        };
        var documents = await PhaseJobs.LoadDocumentsAsync(db, storage, projectId);
        var created = PhaseJobs.Create(project, documents, PhaseKind.Suggestions, input, answered.Select(q => q.Code).ToList(),
            options, provider.Name, Random.Shared, pending.Select(q => q.Code).ToList());
        return await PhaseJobs.EnqueueAsync(db, queue, storage, created);
    }

    // A suggestions job of the project still has a run queued or running.
    private static async Task<bool> SuggestionsRunningAsync(AppDbContext db, int projectId)
    {
        var kind = PhasePlan.JobKind(PhaseKind.Suggestions);
        var jobs = db.Jobs.Where(j => j.ProjectId == projectId && j.Kind == kind).Select(j => j.Id);
        return await db.Runs.AnyAsync(r => r.JobId != null && jobs.Contains(r.JobId.Value)
                                           && (r.State == Core.Runs.RunState.Queued || r.State == Core.Runs.RunState.Running));
    }

    // Called by JobWorker when a round ends: its questions, renumbered after
    // those the project already has, are stored as pending.
    public static async Task StoreRoundAsync(AppDbContext db, Guid jobId, int projectId, PhasePlan plan, PhaseJobResult result)
    {
        var codes = await db.Questions.Where(q => q.ProjectId == projectId).Select(q => q.Code).ToListAsync();
        db.Questions.AddRange(QuestionRules.FromRound(result, plan, projectId, jobId, codes, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }
}
