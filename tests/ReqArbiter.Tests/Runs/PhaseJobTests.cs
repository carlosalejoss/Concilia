using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Phases;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Tests.Runs;

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
                new PhaseExecutor(provider, Events, Storage, runnerOptions, catalogOptions));
        }

        public RunEventStore Events { get; } = new();
        public FileStorage Storage { get; }
        public JobRunner Runner { get; }
        public List<RunOutcome> Finished { get; } = [];
        public List<string> Order { get; } = []; // "start A", "end A"...

        public NewPhaseJob Create(PhaseKind kind, PhaseInput? input = null, IReadOnlyList<string>? answered = null) =>
            PhaseJobs.Create(Project, Documents(), kind, input ?? new PhaseInput { SystemName = "", Language = "" },
                             answered ?? [], new RunnerOptions(), "Fake", new Random(7));

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
        var huge = PhaseJobs.Create(Project, [new CatalogDocument("DOC-01", "enorme.txt", string.Concat(Enumerable.Repeat("Una frase de relleno para el documento. ", 10_000)))],
                                    PhaseKind.Questions, new PhaseInput { SystemName = "", Language = "" }, [], new RunnerOptions(), "Fake", new Random(1));

        Assert.Contains("no tiene archivos con texto", empty.Error);
        Assert.Contains("demasiado grande", huge.Error);
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
        Assert.Equal(File.ReadAllText(firstOutput), File.ReadAllText(Path.Combine(arbiter, PhaseTask.Proposal1File)));

        // The result is the arbiter's output, and it is valid for the arbiter's schema.
        var context = created.Plan.Context(File.ReadAllText(Path.Combine(arbiter, PhaseTask.Proposal1File)),
                                           File.ReadAllText(Path.Combine(arbiter, PhaseTask.Proposal2File)));
        Assert.True(PhaseValidator.Validate(PhaseDefinition.For(kind, PhaseRole.Arbiter), result.Output!, context).Valid);
    }

    [Fact]
    public async Task If_only_one_model_ends_well_its_output_is_the_result_without_cross_check()
    {
        using var setup = new Setup(new FailingModels("SS - GPT-6-LUNA"));

        var result = await setup.RunAsync(setup.Create(PhaseKind.Questions));

        Assert.Equal("A", result.Source);
        Assert.False(result.CrossChecked);
        Assert.NotNull(result.Output);
        var arbiter = setup.Outcome("ARB");
        Assert.Null(arbiter.Attempts);
        Assert.Contains("sin verificación cruzada", arbiter.SkipReason);
        Assert.DoesNotContain("start ARB", setup.Order);
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
}
