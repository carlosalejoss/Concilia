using System.Text.Json;
using System.Text.Json.Nodes;
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

// Steps 27 and 31 (catalog v1.3): a lot is repaired only for errors of the lot
// (missing, not JSON, broken structure, Write cut, C10), more than 20 % of its
// requirements rejected, or a capability without a requirement (C20). A
// usable catalog makes the attempt Done; "bloqueada", timeouts and API errors
// are not retried; one repair by default (Catalog:MaxRepairs).
public class CatalogReviewTests
{
    private const string System = "el sistema ReservaSalas";
    private const string OriginalPrompt = "PROMPT ORIGINAL\n";

    private static string ExamplePath(string file) => Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", file);
    private static string ExampleCatalog => ExamplePath("catalogo_ejemplo_v1.3.json");

    private static readonly RunResult Done = new() { Status = RunStatus.Done, Verdict = new Verdict(VerdictState.Done, null) };

    // The example catalog with a change.
    private static string Mutated(Action<JsonObject> mutate)
    {
        var catalog = JsonNode.Parse(File.ReadAllText(ExampleCatalog))!.AsObject();
        mutate(catalog);
        return catalog.ToJsonString();
    }

    private static JsonObject Req(JsonObject catalog, string id) =>
        catalog["requirements"]!.AsArray().Single(r => r!["id"]!.GetValue<string>() == id)!.AsObject();

    // A run folder with the example's fragments and, if given, a catalog.
    private static RunRequest Attempt(TempDirectory temp, string? catalogJson)
    {
        var folder = Path.Combine(temp.Path, Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(folder, "input"));
        File.Copy(ExamplePath("fragmentos.json"), Path.Combine(folder, CatalogTask.FragmentsFile));
        if (catalogJson is not null)
        {
            Directory.CreateDirectory(Path.Combine(folder, "output"));
            File.WriteAllText(Path.Combine(folder, CatalogTask.OutputFile), catalogJson);
        }
        return new RunRequest { RunId = Guid.NewGuid(), ModelId = "SS - GPT-6-SOL", Prompt = OriginalPrompt, WorkDirectory = folder };
    }

    private static AttemptReview Review(RunRequest request, RunResult result, int attemptsSoFar = 1) =>
        CatalogReview.Review(new RunAttempt(request, result, null), attemptsSoFar, OriginalPrompt, System);

    private static JsonObject Accepted(RunRequest request) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(request.WorkDirectory, CatalogReport.AcceptedFile)))!.AsObject();

    [Fact]
    public void A_valid_catalog_is_kept_and_not_repaired()
    {
        using var temp = new TempDirectory();
        var request = Attempt(temp, File.ReadAllText(ExampleCatalog));

        var review = Review(request, Done);

        Assert.Equal(RunStatus.Done, review.Result.Status);
        Assert.Null(review.RetryPrompt);
        Assert.True(File.Exists(Path.Combine(request.WorkDirectory, CatalogReport.ReportFile)));
        Assert.Equal(9, Accepted(request)["requirements"]!.AsArray().Count);
    }

    [Fact]
    public void Warnings_alone_are_not_repaired()
    {
        using var temp = new TempDirectory();
        var catalog = Mutated(c => Req(c, "RF-005")["obligation_evidence"] = "obligatoriamente"); // C13

        var review = Review(Attempt(temp, catalog), Done);

        Assert.Equal(RunStatus.Done, review.Result.Status);
        Assert.Null(review.RetryPrompt);
    }

    [Fact]
    public void One_rejected_requirement_leaves_the_catalog_without_it_and_is_not_repaired()
    {
        using var temp = new TempDirectory();
        var request = Attempt(temp, Mutated(c => Req(c, "RF-004")["slots"]!["response"] = "mostrar al empleado un aviso si procede"));

        var review = Review(request, Done);

        Assert.Equal(RunStatus.Done, review.Result.Status);
        Assert.Null(review.RetryPrompt);
        var accepted = Accepted(request)["requirements"]!.AsArray();
        Assert.Equal(8, accepted.Count);
        // RF-004 (the holiday notice) was the only one citing DOC-01#F007.
        Assert.DoesNotContain(accepted, r => r!["source_trace"]![0]!["fragment"]!.GetValue<string>() == "DOC-01#F007");
    }

    [Fact]
    public void More_than_20_percent_rejected_asks_for_a_repair_but_keeps_the_catalog()
    {
        using var temp = new TempDirectory();
        var request = Attempt(temp, Mutated(c =>
        {
            Req(c, "RF-003")["slots"]!["response"] = "no permitir a los empleados añadir catering";
            Req(c, "RF-004")["slots"]!["response"] = "mostrar al empleado un aviso si procede";
        }));

        var review = Review(request, Done);

        Assert.Equal(RunStatus.Done, review.Result.Status);
        Assert.StartsWith("Se pide una reparación", review.Result.ErrorMessage);
        Assert.StartsWith(OriginalPrompt, review.RetryPrompt);
        Assert.Contains("# §12 REPARACIÓN (intento 1)", review.RetryPrompt);
        Assert.Contains("RF-003 C15", review.RetryPrompt);
        Assert.Contains("RF-004 C5", review.RetryPrompt);
    }

    [Fact]
    public void A_capability_without_a_requirement_asks_for_a_repair()
    {
        using var temp = new TempDirectory();
        var request = Attempt(temp, Mutated(c => Req(c, "RF-006")["capability_ids"] = new JsonArray()));

        var review = Review(request, Done);

        Assert.Equal(RunStatus.Done, review.Result.Status);
        Assert.Contains("CAP-005 C20", review.RetryPrompt);
    }

    [Fact]
    public void A_missing_catalog_fails_and_is_repaired()
    {
        using var temp = new TempDirectory();
        // "hecho", but the file is not there: RunEvaluator fails it.
        var result = new RunResult { Status = RunStatus.Failed, Verdict = new Verdict(VerdictState.Done, null), ErrorMessage = "Faltan archivos de salida" };

        var review = Review(Attempt(temp, null), result);

        Assert.Equal(RunStatus.Failed, review.Result.Status);
        Assert.Contains($"LOTE Falta el fichero {CatalogTask.OutputFile}", review.RetryPrompt);
    }

    [Fact]
    public void A_catalog_with_errors_of_the_lot_fails_and_is_repaired()
    {
        using var temp = new TempDirectory();

        var review = Review(Attempt(temp, Mutated(c => c["schema_version"] = "1.2")), Done);

        Assert.Equal(RunStatus.Failed, review.Result.Status);
        Assert.StartsWith("Catálogo no válido: LOTE SCHEMA", review.Result.ErrorMessage);
        Assert.Contains("<catalogo_previo>{", review.RetryPrompt);
    }

    [Fact]
    public void A_truncated_write_is_repaired_and_says_why()
    {
        using var temp = new TempDirectory();
        var result = new RunResult { Status = RunStatus.Failed, ErrorMessage = ClaudeCliRunner.TruncatedToolCall };

        var review = Review(Attempt(temp, null), result);

        Assert.Equal(ClaudeCliRunner.TruncatedToolCall, review.Result.ErrorMessage);
        Assert.Contains("LOTE " + ClaudeCliRunner.TruncatedToolCall, review.RetryPrompt);
    }

    [Fact]
    public void A_valid_catalog_with_a_bad_verdict_line_is_accepted_without_repeating_the_lot()
    {
        using var temp = new TempDirectory();
        var result = new RunResult { Status = RunStatus.Failed, VerdictError = "No se ha encontrado ninguna línea." };

        var review = Review(Attempt(temp, File.ReadAllText(ExampleCatalog)), result);

        Assert.Equal(RunStatus.Done, review.Result.Status);
        Assert.Null(review.Result.VerdictError);
        Assert.Contains("Veredicto no válido", review.Result.ErrorMessage);
        Assert.Null(review.RetryPrompt);
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
    private static (RunExecutor Executor, Run Run, JobContext Context) Executor(TempDirectory temp, IRunnerProvider provider, int maxRepairs = 1)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = temp.Path })
            .Build();
        var storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = temp.Path });
        var options = new CatalogOptions { MaxRepairs = maxRepairs };
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

        Assert.Equal([RunStatus.Failed, RunStatus.Done], attempts.Select(a => a.Result.Status));
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

    // A catalog with an error of the lot, saved where FakeClaude can copy it.
    private static string BrokenCatalogFile(TempDirectory temp)
    {
        var path = Path.Combine(temp.Path, "roto.json");
        File.WriteAllText(path, Mutated(c => c["schema_version"] = "1.2"));
        return path;
    }

    // End to end with FakeClaude: the first attempt copies a broken catalog and
    // the repair (its prompt carries the annex) copies the valid example.
    [Fact]
    public async Task With_fake_claude_a_broken_catalog_is_repaired()
    {
        using var temp = new TempDirectory();
        var directive = Directive("tools-sol.jsonl",
            copyFiles: new() { [CatalogTask.OutputFile] = BrokenCatalogFile(temp) },
            repairCopyFiles: new() { [CatalogTask.OutputFile] = ExampleCatalog });
        var (executor, run, context) = Executor(temp, new DirectiveProvider(new ClaudeCliRunner(ExecutablePath, FakeGateway), directive));

        var attempts = await executor.ExecuteAsync(run, context, CancellationToken.None);

        Assert.Equal([RunStatus.Failed, RunStatus.Done], attempts.Select(a => a.Result.Status));
    }

    // A catalog that stays broken gets one repair by default, and as many as
    // Catalog:MaxRepairs says.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task With_fake_claude_a_catalog_that_stays_broken_gets_max_repairs(int maxRepairs)
    {
        using var temp = new TempDirectory();
        var directive = Directive("tools-sol.jsonl", copyFiles: new() { [CatalogTask.OutputFile] = BrokenCatalogFile(temp) });
        var (executor, run, context) = Executor(temp, new DirectiveProvider(new ClaudeCliRunner(ExecutablePath, FakeGateway), directive), maxRepairs);

        var attempts = await executor.ExecuteAsync(run, context, CancellationToken.None);

        Assert.Equal(1 + maxRepairs, attempts.Count);
        Assert.All(attempts, a => Assert.Equal(RunStatus.Failed, a.Result.Status));
        Assert.Contains($"intento {maxRepairs})", attempts[^1].Request.Prompt);
    }
}
