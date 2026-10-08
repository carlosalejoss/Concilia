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
