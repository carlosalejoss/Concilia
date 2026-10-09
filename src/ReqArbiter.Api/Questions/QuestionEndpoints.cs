using Microsoft.EntityFrameworkCore;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Persistence;
using ReqArbiter.Api.Projects;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Flow;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Questions;
using ReqArbiter.Core.Runner;

namespace ReqArbiter.Api.Questions;

public record AnswerRequest(string? Answer);
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
            ApplyAsync(db, id, q => QuestionRules.Answer(q, body.Answer, DateTimeOffset.UtcNow)));

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

    // Called by JobWorker when a round ends: its questions, renumbered after
    // those the project already has, are stored as pending.
    public static async Task StoreRoundAsync(AppDbContext db, Guid jobId, int projectId, PhasePlan plan, PhaseJobResult result)
    {
        var codes = await db.Questions.Where(q => q.ProjectId == projectId).Select(q => q.Code).ToListAsync();
        db.Questions.AddRange(QuestionRules.FromRound(result, plan, projectId, jobId, codes, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }
}
