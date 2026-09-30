using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;
using ReqArbiter.Runner;
using static ReqArbiter.Tests.Runner.FakeClaudeLauncher;

namespace ReqArbiter.Tests.Runs;

// Step 27: an attempt whose catalog is missing, is not JSON or has errors is
// repaired (original prompt + annex); warnings, "bloqueada", timeouts and API
// errors are not retried; at most two retries in total.
public class CatalogReviewTests
{
    private const string System = "el sistema ReservaSalas";
    private const string OriginalPrompt = "PROMPT ORIGINAL\n";

    private static string ExamplePath(string file) => Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", file);
    private static string MutationPath(string file) => Path.Combine(AppContext.BaseDirectory, "fixtures", "catalog-mutations", file);

    private static readonly RunResult Done = new() { Status = RunStatus.Done, Verdict = new Verdict(VerdictState.Done, null) };

    // A run folder with the example's fragments and, if given, a catalog.
    private static RunRequest Attempt(TempDirectory temp, string? catalogFile)
    {
        var folder = Path.Combine(temp.Path, Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(folder, "input"));
        File.Copy(ExamplePath("fragmentos.json"), Path.Combine(folder, CatalogTask.FragmentsFile));
        if (catalogFile is not null)
        {
            Directory.CreateDirectory(Path.Combine(folder, "output"));
            File.Copy(catalogFile, Path.Combine(folder, CatalogTask.OutputFile));
        }
        return new RunRequest { RunId = Guid.NewGuid(), ModelId = "SS - GPT-6-SOL", Prompt = OriginalPrompt, WorkDirectory = folder };
    }

    private static AttemptReview Review(RunRequest request, RunResult result, int attemptsSoFar = 1) =>
        CatalogReview.Review(new RunAttempt(request, result, null), attemptsSoFar, OriginalPrompt, System);

    [Fact]
    public void A_valid_catalog_is_kept_and_not_retried()
    {
        using var temp = new TempDirectory();
        var request = Attempt(temp, ExamplePath("catalogo_ejemplo.json"));

        var review = Review(request, Done);

        Assert.Equal(RunStatus.Done, review.Result.Status);
        Assert.Null(review.RetryPrompt);
        Assert.True(File.Exists(Path.Combine(request.WorkDirectory, CatalogReport.ReportFile)));
    }

    [Fact]
    public void Warnings_alone_are_not_retried()
    {
        using var temp = new TempDirectory();

        var review = Review(Attempt(temp, MutationPath("warn-01.json")), Done);

        Assert.Equal(RunStatus.Done, review.Result.Status);
        Assert.Null(review.RetryPrompt);
    }

    [Fact]
    public void An_invalid_catalog_fails_and_is_repaired_with_the_original_prompt_and_the_annex()
    {
        using var temp = new TempDirectory();
        var request = Attempt(temp, MutationPath("mut-01.json"));

        var review = Review(request, Done, attemptsSoFar: 1);

        Assert.Equal(RunStatus.Failed, review.Result.Status);
        Assert.StartsWith("Catálogo no válido", review.Result.ErrorMessage);
        Assert.StartsWith(OriginalPrompt, review.RetryPrompt);
        Assert.Contains("intento 1 de 2", review.RetryPrompt);
        Assert.Contains("<catalogo_previo>" + File.ReadAllText(MutationPath("mut-01.json")) + "</catalogo_previo>", review.RetryPrompt);
        Assert.Contains(" C12 ", review.RetryPrompt); // the expected code of mut-01, one error per line
    }

    [Fact]
    public void A_missing_catalog_is_repaired()
    {
        using var temp = new TempDirectory();
        // "hecho", but the file is not there: RunEvaluator fails it.
        var result = new RunResult { Status = RunStatus.Failed, Verdict = new Verdict(VerdictState.Done, null), ErrorMessage = "Faltan archivos de salida" };

        var review = Review(Attempt(temp, null), result, attemptsSoFar: 2);

        Assert.Equal(RunStatus.Failed, review.Result.Status);
        Assert.Contains("intento 2 de 2", review.RetryPrompt);
        Assert.Contains($"Falta el fichero {CatalogTask.OutputFile}", review.RetryPrompt);
    }

    [Fact]
    public void A_truncated_write_is_repaired_and_says_why()
    {
        using var temp = new TempDirectory();
        var result = new RunResult { Status = RunStatus.Failed, ErrorMessage = ClaudeCliRunner.TruncatedToolCall };

        var review = Review(Attempt(temp, null), result);

        Assert.Equal(ClaudeCliRunner.TruncatedToolCall, review.Result.ErrorMessage);
        Assert.Contains(ClaudeCliRunner.TruncatedToolCall, review.RetryPrompt);
    }

    [Fact]
    public void A_valid_catalog_with_a_bad_verdict_only_asks_for_the_verdict()
    {
        using var temp = new TempDirectory();
        var result = new RunResult { Status = RunStatus.Failed, VerdictError = "No se ha encontrado ninguna línea." };

        var review = Review(Attempt(temp, ExamplePath("catalogo_ejemplo.json")), result);

        Assert.Equal(OriginalPrompt + RetryRunner.RetryNote(result.VerdictError), review.RetryPrompt);
    }

    public static TheoryData<RunResult> NotRetried =>
    [
        new RunResult { Status = RunStatus.Blocked, Verdict = new Verdict(VerdictState.Blocked, "Falta la documentación") },
        new RunResult { Status = RunStatus.TimedOut, ErrorMessage = "Se agotó el tiempo máximo" },
        new RunResult { Status = RunStatus.HeartbeatLost },
        new RunResult { Status = RunStatus.Failed, IsError = true, ErrorMessage = "Error de la API 403" },
    ];

    [Theory]
    [MemberData(nameof(NotRetried))]
    public void Blocked_timeouts_and_api_errors_are_not_retried(RunResult result)
    {
        using var temp = new TempDirectory();

        var review = Review(Attempt(temp, null), result);

        Assert.Same(result, review.Result);
        Assert.Null(review.RetryPrompt);
    }

    // RunExecutor over a temp folder, with the example as the only document.
    private static (RunExecutor Executor, Run Run, JobContext Context) Executor(TempDirectory temp, IRunnerProvider provider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = temp.Path })
            .Build();
        var storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = temp.Path });
        var options = new CatalogOptions();
        var plan = CatalogTask.Plan([new CatalogDocument("DOC-01", "DOC-01.txt", File.ReadAllText(ExamplePath(Path.Combine("fuentes", "DOC-01.txt"))))], options);
        var project = new Project { Id = 1, Name = "ReservaSalas", SystemName = System, Language = "es" };
        var run = new Run
        {
            Id = Guid.NewGuid(), ProjectId = 1, JobId = Guid.NewGuid(), Slot = "A", LotNumber = 1, LotTotal = plan.Lots.Count,
            Provider = "Test", Model = "SS - GPT-6-SOL", State = RunState.Queued,
        };
        return (new RunExecutor(provider, new RunEventStore(), storage, new RunnerOptions(), options), run, new JobContext(project, plan));
    }

    [Fact]
    public async Task With_fake_provider_a_simulated_repair_ends_with_a_valid_catalog()
    {
        using var temp = new TempDirectory();
        var (executor, run, context) = Executor(temp, new FakeProvider(simulateRepair: true));

        var attempts = await executor.ExecuteAsync(run, context, CancellationToken.None);

        Assert.Equal(2, attempts.Count);
        Assert.Equal(RunStatus.Failed, attempts[0].Result.Status);
        Assert.Equal(RunStatus.Done, attempts[1].Result.Status);
        Assert.Equal(run.Id, attempts[1].PreviousRunId);
        Assert.Contains("<catalogo_previo>", attempts[1].Request.Prompt);
    }

    // Puts the "#fake" directive in front of every prompt (retries included),
    // so FakeClaude knows what to do.
    private sealed class DirectiveProvider(IRunnerProvider inner, string directive) : IRunnerProvider
    {
        public string Name => inner.Name;

        public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken) =>
            inner.RunAsync(request with { Prompt = directive + request.Prompt }, onEvent, cancellationToken);
    }

    // End to end with FakeClaude: the first attempt copies a mutated catalog
    // and the repair (its prompt carries the annex) copies the valid example.
    [Fact]
    public async Task With_fake_claude_an_invalid_catalog_is_repaired()
    {
        using var temp = new TempDirectory();
        var directive = Directive("tools-sol.jsonl",
            copyFiles: new() { [CatalogTask.OutputFile] = MutationPath("mut-01.json") },
            repairCopyFiles: new() { [CatalogTask.OutputFile] = ExamplePath("catalogo_ejemplo.json") });
        var (executor, run, context) = Executor(temp, new DirectiveProvider(new ClaudeCliRunner(ExecutablePath, FakeGateway), directive));

        var attempts = await executor.ExecuteAsync(run, context, CancellationToken.None);

        Assert.Equal([RunStatus.Failed, RunStatus.Done], attempts.Select(a => a.Result.Status));
    }

    // A catalog that stays invalid uses the two retries and the lot ends Failed.
    [Fact]
    public async Task With_fake_claude_a_catalog_that_stays_invalid_fails_after_two_retries()
    {
        using var temp = new TempDirectory();
        var directive = Directive("tools-sol.jsonl", copyFiles: new() { [CatalogTask.OutputFile] = MutationPath("mut-01.json") });
        var (executor, run, context) = Executor(temp, new DirectiveProvider(new ClaudeCliRunner(ExecutablePath, FakeGateway), directive));

        var attempts = await executor.ExecuteAsync(run, context, CancellationToken.None);

        Assert.Equal(1 + RetryRunner.MaxRetries, attempts.Count);
        Assert.All(attempts, a => Assert.Equal(RunStatus.Failed, a.Result.Status));
        Assert.Contains("intento 2 de 2", attempts[^1].Request.Prompt);
    }
}
