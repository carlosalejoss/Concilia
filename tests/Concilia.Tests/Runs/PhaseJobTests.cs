using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Concilia.Api.Files;
using Concilia.Api.Runs;
using Concilia.Core.Catalog;
using Concilia.Core.Embeddings;
using Concilia.Core.Matching;
using Concilia.Core.Phases;
using Concilia.Core.Projects;
using Concilia.Core.Runner;
using Concilia.Core.Runs;

namespace Concilia.Tests.Runs;

// A v0 phase job without the database (step 43), with ReservaSalas and the
// FakeProvider: A and B in parallel and then the arbiter with their outputs
// in the random order of the plan; and what happens when one or both fail.
public class PhaseJobTests
{
    private static readonly Project Project = new() { Id = 1, Name = "ReservaSalas", SystemName = "el sistema ReservaSalas" };

    private static List<CatalogDocument> Documents() =>
        [new("DOC-01", "DOC-01.txt", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt")))];

    private static string Example(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts-v0", "ejemplo", file));

    private sealed class Setup : IDisposable
    {
        private readonly TempDirectory _temp = new();

        public Setup(IRunnerProvider provider)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = _temp.Path })
                .Build();
            Storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = _temp.Path });
            var catalogOptions = new CatalogOptions();
            var runnerOptions = new RunnerOptions();
            Runner = new JobRunner(
                new RunExecutor(provider, Events, Storage, runnerOptions, catalogOptions), catalogOptions,
                new PhaseExecutor(provider, Events, Storage, runnerOptions, catalogOptions,
                    new ArbiterMatching(new FakeTextEmbedder(), new MatchThresholds(0.9, 0.7))));
        }

        public RunEventStore Events { get; } = new();
        public FileStorage Storage { get; }
        public JobRunner Runner { get; }
        public List<RunOutcome> Finished { get; } = [];
        public List<string> Order { get; } = []; // "start A", "end A"...

        public NewPhaseJob Create(PhaseKind kind, PhaseInput? input = null, IReadOnlyList<string>? answered = null,
                                  IReadOnlyList<string>? pending = null) =>
            PhaseJobs.Create(Project, Documents(), kind, input ?? new PhaseInput { SystemName = "", Language = "" },
                             answered ?? [], new RunnerOptions(), "Fake", new Random(7), pending);

        public async Task<PhaseJobResult> RunAsync(NewPhaseJob created)
        {
            created.Plan!.Save(Storage.JobFolder(created.Job!.Id));
            var plan = PhasePlan.Load(Storage.JobFolder(created.Job.Id));
            return await Runner.RunPhaseAsync(created.Runs, plan,
                run => { lock (Order) Order.Add($"start {run.Slot}"); return Task.CompletedTask; },
                outcome => { lock (Order) { Order.Add($"end {outcome.Run.Slot}"); Finished.Add(outcome); } return Task.CompletedTask; },
                CancellationToken.None);
        }

        public RunOutcome Outcome(string slot) => Finished.Single(o => o.Run.Slot == slot);

        public void Dispose() => _temp.Dispose();
    }

    // Throws on every run of the given models; delegates the rest to FakeProvider.
    private sealed class FailingModels(params string[] failing) : IRunnerProvider
    {
        private readonly FakeProvider _fake = new();
        public string Name => "Test";

        public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken) =>
            failing.Contains(request.ModelId)
                ? Task.FromResult(new RunResult { Status = RunStatus.Failed, IsError = true, ErrorMessage = "403 (simulado)" })
                : _fake.RunAsync(request, onEvent, cancellationToken);
    }

    // Like FakeProvider, but the given model writes its output and then runs
    // out of time before the verdict.
    private sealed class TimesOutAfterWriting(string model) : IRunnerProvider
    {
        private readonly FakeProvider _fake = new();
        public string Name => "Test";

        public async Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
        {
            var result = await _fake.RunAsync(request, onEvent, cancellationToken);
            return request.ModelId == model
                ? result with { Status = RunStatus.TimedOut, Verdict = null, ErrorMessage = "Se agotó el tiempo máximo (00:30:00)." }
                : result;
        }
    }

    // Like FakeProvider, but the first attempt of every run reports no reads,
    // as a model that wrote its output without reading its input.
    private sealed class SkipsReadingFirst : IRunnerProvider
    {
        private readonly FakeProvider _fake = new();
        public string Name => "Test";

        public async Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
        {
            var result = await _fake.RunAsync(request, onEvent, cancellationToken);
            return request.Prompt.Contains(FakePhaseOutput.RepairMarker) ? result : result with { Reads = [] };
        }
    }

    [Fact]
    public async Task A_run_that_left_lines_unread_is_repaired()
    {
        using var setup = new Setup(new SkipsReadingFirst());

        var result = await setup.RunAsync(setup.Create(PhaseKind.Questions));

        Assert.Equal("ARB", result.Source);
        foreach (var slot in new[] { "A", "B", "ARB" })
        {
            var attempts = setup.Outcome(slot).Attempts!;
            Assert.Equal([RunStatus.Failed, RunStatus.Done], attempts.Select(a => a.Result.Status));
            Assert.Contains("LECTURA: no has leído las líneas", attempts[0].Result.ErrorMessage);
        }
    }

    [Fact]
    public void A_job_has_three_runs_and_a_plan_with_a_random_order()
    {
        var created = new Setup(new FakeProvider()).Create(PhaseKind.Questions);

        Assert.Null(created.Error);
        Assert.Equal("questions", created.Job!.Kind);
        Assert.Equal(["A", "B", "ARB"], created.Runs.Select(r => r.Slot));
        Assert.Equal(["SS - GPT-6-SOL", "SS - GPT-6-LUNA", "SS - GPT-6-SOL"], created.Runs.Select(r => r.Model));
        Assert.Equal(["A", "B"], created.Plan!.ProposalOrder.Order());
        Assert.Equal(14, created.Plan.Fragments.Count);
        Assert.Equal("el sistema ReservaSalas", created.Plan.Input.SystemName);
    }

    [Fact]
    public void Both_orders_come_out_and_the_plan_keeps_its_own()
    {
        var orders = Enumerable.Range(0, 40).Select(i => string.Join(",", PhasePlan.RandomOrder(new Random(i)))).ToHashSet();
        Assert.Equal(["A,B", "B,A"], orders.Order());

        using var temp = new TempDirectory();
        var plan = new Setup(new FakeProvider()).Create(PhaseKind.Catalog).Plan!;
        plan.Save(temp.Path);
        Assert.Equal(plan.ProposalOrder, PhasePlan.Load(temp.Path).ProposalOrder);
    }

    [Fact]
    public void The_arbiter_translates_its_proposals_back_to_A_and_B()
    {
        var plan = new Setup(new FakeProvider()).Create(PhaseKind.Questions).Plan! with { ProposalOrder = ["B", "A"] };

        Assert.Equal("B", plan.OriginOf(JsonNode.Parse("""{"propuesta_1":["P-001"],"propuesta_2":[]}""")!.AsObject()));
        Assert.Equal("A", plan.OriginOf(JsonNode.Parse("""{"propuesta_1":[],"propuesta_2":["P-003"]}""")!.AsObject()));
        Assert.Equal("ambas", plan.OriginOf(JsonNode.Parse("""{"propuesta_1":["P-001"],"propuesta_2":["P-002"]}""")!.AsObject()));
    }

    [Fact]
    public void Without_documents_or_over_the_budget_no_job_is_created()
    {
        var setup = new Setup(new FakeProvider());

        var empty = PhaseJobs.Create(Project, [], PhaseKind.Questions, new PhaseInput { SystemName = "", Language = "" }, [], new RunnerOptions(), "Fake", new Random(1));
        // About 120,000 tokens: read by parts it is fine with the default
        // limit (300,000), and refused with a lower one.
        var document = new CatalogDocument("DOC-01", "enorme.txt", string.Concat(Enumerable.Repeat("Una frase de relleno para el documento. ", 10_000)));
        var fits = PhaseJobs.Create(Project, [document], PhaseKind.Questions, new PhaseInput { SystemName = "", Language = "" }, [], new RunnerOptions(), "Fake", new Random(1));
        var huge = PhaseJobs.Create(Project, [document], PhaseKind.Questions, new PhaseInput { SystemName = "", Language = "" }, [],
                                    new RunnerOptions { MaxDocumentationTokens = 50_000 }, "Fake", new Random(1));

        Assert.Contains("no tiene archivos con texto", empty.Error);
        Assert.Null(fits.Error);
        Assert.Contains("La documentación del proyecto es demasiado grande", huge.Error);
        Assert.Null(huge.Job);
    }

    public static TheoryData<PhaseKind> Phases() => [PhaseKind.Questions, PhaseKind.Catalog, PhaseKind.Backlog];

    [Theory]
    [MemberData(nameof(Phases))]
    public async Task A_and_B_run_first_and_then_the_arbiter_joins_them(PhaseKind kind)
    {
        using var setup = new Setup(new FakeProvider());
        var input = new PhaseInput { SystemName = "", Language = "", Catalog = kind == PhaseKind.Backlog ? Example("catalogo_arbitro.json") : "" };
        var created = setup.Create(kind, input);

        var result = await setup.RunAsync(created);

        Assert.Equal("ARB", result.Source);
        Assert.True(result.CrossChecked);
        Assert.Equal(["start ARB", "end ARB"], setup.Order.Skip(4));
        Assert.All(setup.Finished, o => Assert.True(o.Succeeded, o.Attempts?[^1].Result.ErrorMessage));

        // The arbiter received the outputs of A and B in the order of the plan.
        var arbiter = setup.Outcome("ARB").Attempts![0].Request.WorkDirectory;
        var first = created.Plan!.ProposalOrder[0];
        var firstOutput = PhaseExecutor.OutputPath(setup.Outcome(first).Attempts![^1], kind);
        // (indented, so Read does not cut a long line, and with "1:" before
        // each id, so the arbiter cannot mix the proposals: the same JSON otherwise).
        var proposal1 = File.ReadAllText(Path.Combine(arbiter, PhaseTask.Proposal1File));
        Assert.Contains("\"id\":\"1:", proposal1);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(firstOutput)), JsonNode.Parse(ProposalIds.Unprefix(kind, proposal1))));
        // Every run read its input by parts (FakeProvider reads every file of input/).
        Assert.All(setup.Finished, o => Assert.NotEmpty(o.Attempts![^1].Result.Reads));

        // Step 88: the arbiter also got the matching of the platform, in its reading plan.
        var matching = File.ReadAllText(Path.Combine(arbiter, MatchingFile.Path));
        Assert.Matches(@"^1:\S+ (2:\S+ (mismo|parecido) \d\.\d\d|sin-par)$", matching.Split('\n')[0]);
        Assert.Contains(MatchingFile.Path, setup.Outcome("ARB").Attempts![0].Request.Prompt);
        Assert.Contains(setup.Outcome("ARB").Attempts![^1].Result.Reads, r => r.Path == MatchingFile.Path);

        // The result is the arbiter's output, kept without the prefixes, and
        // it is valid for the arbiter's schema.
        Assert.DoesNotContain("\"1:", result.Output);
        var context = created.Plan.Context(ProposalIds.Unprefix(kind, proposal1),
                                           ProposalIds.Unprefix(kind, File.ReadAllText(Path.Combine(arbiter, PhaseTask.Proposal2File))));
        Assert.True(PhaseValidator.Validate(PhaseDefinition.For(kind, PhaseRole.Arbiter), result.Output!, context).Valid);
    }

    [Fact]
    public async Task If_only_one_model_ends_well_the_job_fails_without_its_output()
    {
        using var setup = new Setup(new FailingModels("SS - GPT-6-LUNA"));

        var result = await setup.RunAsync(setup.Create(PhaseKind.Questions));

        // Decision of 14-10-2026: the output of a single model is not the result.
        Assert.Null(result.Source);
        Assert.Null(result.Output);
        var arbiter = setup.Outcome("ARB");
        Assert.Null(arbiter.Attempts);
        Assert.Contains("solo A terminó con una salida válida", arbiter.SkipReason);
        Assert.DoesNotContain("start ARB", setup.Order);
    }

    [Fact]
    public async Task A_valid_output_written_before_the_time_ran_out_is_accepted()
    {
        using var setup = new Setup(new TimesOutAfterWriting("SS - GPT-6-SOL"));

        var result = await setup.RunAsync(setup.Create(PhaseKind.Questions));

        Assert.Equal("ARB", result.Source);
        var arbiter = setup.Outcome("ARB").Attempts!;
        Assert.Equal(RunStatus.Done, Assert.Single(arbiter).Result.Status); // accepted, without a repair
        Assert.Contains("la salida ya estaba escrita y es válida", arbiter[0].Result.ErrorMessage);
    }

    [Fact]
    public async Task If_both_models_fail_the_job_fails_and_the_arbiter_is_not_called()
    {
        using var setup = new Setup(new FailingModels("SS - GPT-6-SOL", "SS - GPT-6-LUNA"));

        var result = await setup.RunAsync(setup.Create(PhaseKind.Questions));

        Assert.Null(result.Source);
        Assert.Null(result.Output);
        Assert.Contains("ni A ni B", setup.Outcome("ARB").SkipReason);
        Assert.Equal(["start A", "start B"], setup.Order.Where(o => o.StartsWith("start")).Order());
    }

    [Fact]
    public async Task An_invalid_output_is_repaired_once()
    {
        using var setup = new Setup(new FakeProvider(simulateRepair: true));

        var result = await setup.RunAsync(setup.Create(PhaseKind.Catalog));

        Assert.Equal("ARB", result.Source);
        foreach (var slot in new[] { "A", "B", "ARB" })
        {
            var attempts = setup.Outcome(slot).Attempts!;
            Assert.Equal([RunStatus.Failed, RunStatus.Done], attempts.Select(a => a.Result.Status));
            Assert.StartsWith("Salida no válida: JSON no válido", attempts[0].Result.ErrorMessage);
            Assert.Contains("<salida_previa>", attempts[1].Request.Prompt);
        }
    }

    [Fact]
    public void The_result_file_keeps_where_it_comes_from()
    {
        using var temp = new TempDirectory();

        PhaseJobs.WriteResult(temp.Path, new PhaseJobResult("B", false, """{"preguntas":[]}""", "solo B"));

        var saved = JsonNode.Parse(File.ReadAllText(Path.Combine(temp.Path, PhaseJobResult.FileName)))!;
        Assert.Equal("B", saved["fuente"]!.GetValue<string>());
        Assert.False(saved["verificacion_cruzada"]!.GetValue<bool>());
        Assert.Empty(saved["salida"]!["preguntas"]!.AsArray());
    }

    [Fact]
    public async Task The_suggestions_are_a_single_run_over_the_pending_questions()
    {
        using var setup = new Setup(new FakeProvider());
        var input = new PhaseInput
        {
            SystemName = "", Language = "",
            PendingQuestions = PhasePrompt.PendingQuestionsBlock([("P-002", "¿Outlook?", "RI"), ("P-004", "¿Campos del CSV?", "RF")]),
        };
        var created = setup.Create(PhaseKind.Suggestions, input, pending: ["P-002", "P-004"]);

        Assert.Equal("answer-suggestions", created.Job!.Kind);
        var run = Assert.Single(created.Runs);
        Assert.Equal(("SUG", "SS - GPT-6-SOL"), (run.Slot, run.Model));

        created.Plan!.Save(setup.Storage.JobFolder(created.Job.Id));
        var result = await setup.Runner.RunSingleAsync(run, PhasePlan.Load(setup.Storage.JobFolder(created.Job.Id)),
            _ => Task.CompletedTask, _ => Task.CompletedTask, CancellationToken.None);

        Assert.Equal("SUG", result.Source);
        Assert.False(result.CrossChecked);
        var suggested = JsonNode.Parse(result.Output!)!["sugerencias"]!.AsArray().Select(s => s!["pregunta"]!.GetValue<string>());
        Assert.Equal(["P-002", "P-004"], suggested);
    }

    [Fact]
    public async Task If_the_suggestions_model_fails_there_is_no_output()
    {
        using var setup = new Setup(new FailingModels("SS - GPT-6-SOL"));
        var created = setup.Create(PhaseKind.Suggestions, pending: ["P-002"]);
        created.Plan!.Save(setup.Storage.JobFolder(created.Job!.Id));

        var result = await setup.Runner.RunSingleAsync(created.Runs[0], PhasePlan.Load(setup.Storage.JobFolder(created.Job.Id)),
            _ => Task.CompletedTask, _ => Task.CompletedTask, CancellationToken.None);

        Assert.Null(result.Source);
        Assert.Null(result.Output);
    }
}
