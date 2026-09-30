using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Tests.Runs;

// The runs of a job without the database: slots A and B in parallel, lots of
// a slot in parallel up to the limit, isolation between runs, and separate
// folders and streams.
public class JobRunnerTests
{
    private const string ModelA = "SS - GPT-6-SOL";
    private const string ModelB = "SS - GPT-6-LUNA";

    // Everything JobRunner needs, with runs stored under a temp folder and the
    // ReservaSalas example as the only document.
    private sealed class Setup : IDisposable
    {
        private readonly TempDirectory _temp = new();

        public Setup(IRunnerProvider provider, int maxOutputTokensPerLot = 20_000, int maxParallelLots = 5)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = _temp.Path })
                .Build();
            Storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = _temp.Path });
            var catalogOptions = new CatalogOptions { MaxOutputTokensPerLot = maxOutputTokensPerLot, MaxParallelLots = maxParallelLots };
            Runner = new JobRunner(new RunExecutor(provider, Events, Storage, new RunnerOptions(), catalogOptions), catalogOptions);

            var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt"));
            var plan = CatalogTask.Plan([new CatalogDocument("DOC-01", "DOC-01.txt", text)], catalogOptions);
            var project = new Project { Id = 1, Name = "ReservaSalas", SystemName = "el sistema ReservaSalas" };
            Context = new JobContext(project, plan);
        }

        public RunEventStore Events { get; } = new();
        public FileStorage Storage { get; }
        public JobRunner Runner { get; }
        public JobContext Context { get; }

        // One run per slot and lot, as POST /projects/{id}/jobs creates them.
        public List<Run> JobRuns()
        {
            var jobId = Guid.NewGuid();
            Run NewRun(string slot, string model, LotPlan lot) => new()
            {
                Id = Guid.NewGuid(),
                ProjectId = 1,
                JobId = jobId,
                Slot = slot,
                LotNumber = lot.Number,
                LotTotal = lot.Total,
                Provider = "Test",
                Model = model,
                State = RunState.Queued,
            };
            return Context.Plan.Lots.SelectMany(l => new[] { NewRun("A", ModelA, l), NewRun("B", ModelB, l) }).ToList();
        }

        public Task RunAsync(List<Run> runs, Func<RunOutcome, Task> onFinished, Func<Run, Task>? onStarting = null) =>
            Runner.RunAsync(runs, Context, onStarting ?? (_ => Task.CompletedTask), onFinished, CancellationToken.None);

        public void Dispose() => _temp.Dispose();
    }

    // Fails every run of one model; delegates the rest to FakeProvider.
    private sealed class FailingModelProvider(string failingModel) : IRunnerProvider
    {
        private readonly FakeProvider _fake = new();
        public string Name => "Test";

        public Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken) =>
            request.ModelId == failingModel
                ? throw new IOException("Disco lleno (simulado)")
                : _fake.RunAsync(request, onEvent, cancellationToken);
    }

    // Each run waits until `expectedRuns` runs have started, then continues.
    // If the runs went one after the other, the first run would wait forever
    // (here: until the 10 s timeout makes the test fail). It also records the
    // most runs of one model that were ever running at the same time.
    private sealed class BarrierProvider(int expectedRuns) : IRunnerProvider
    {
        private readonly FakeProvider _fake = new();
        private readonly TaskCompletionSource _allStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Dictionary<string, (int Now, int Max)> _perModel = [];
        private int _started;
        public string Name => "Barrier";

        public int MaxAtOnce(string model)
        {
            lock (_perModel)
            {
                return _perModel[model].Max;
            }
        }

        public async Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
        {
            Count(request.ModelId, +1);
            try
            {
                if (Interlocked.Increment(ref _started) == expectedRuns)
                {
                    _allStarted.SetResult();
                }
                await _allStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
                return await _fake.RunAsync(request, onEvent, cancellationToken);
            }
            finally
            {
                Count(request.ModelId, -1);
            }
        }

        private void Count(string model, int change)
        {
            lock (_perModel)
            {
                var (now, max) = _perModel.GetValueOrDefault(model);
                now += change;
                _perModel[model] = (now, Math.Max(now, max));
            }
        }
    }

    [Fact]
    public async Task Slots_a_and_b_execute_at_the_same_time()
    {
        using var setup = new Setup(new BarrierProvider(expectedRuns: 2)); // one lot: runs A1 and B1
        var outcomes = new List<RunOutcome>();

        await setup.RunAsync(setup.JobRuns(), o => { lock (outcomes) outcomes.Add(o); return Task.CompletedTask; });

        // A TimeoutException inside a run would show up as an Error here.
        Assert.Equal(2, outcomes.Count);
        Assert.All(outcomes, o => Assert.Null(o.Error));
        Assert.All(outcomes, o => Assert.Equal(RunStatus.Done, Assert.Single(o.Attempts!).Result.Status));
    }

    [Fact]
    public async Task Up_to_max_parallel_lots_of_a_slot_run_at_the_same_time()
    {
        // A small lot size splits the example into more than 2 lots. The first
        // 2 lots of each slot must run together to pass the barrier, and never
        // more than 2 of the same model.
        var provider = new BarrierProvider(expectedRuns: 4);
        using var setup = new Setup(provider, maxOutputTokensPerLot: 1_500, maxParallelLots: 2);
        var outcomes = new List<RunOutcome>();

        await setup.RunAsync(setup.JobRuns(), o => { lock (outcomes) outcomes.Add(o); return Task.CompletedTask; });

        Assert.True(setup.Context.Plan.Lots.Count > 2);
        Assert.All(outcomes, o => Assert.Null(o.Error));
        Assert.Equal(2, provider.MaxAtOnce(ModelA));
        Assert.Equal(2, provider.MaxAtOnce(ModelB));
    }

    [Fact]
    public async Task With_one_lot_at_a_time_the_lots_of_a_slot_run_in_order()
    {
        // A small lot size splits the example into several lots.
        using var setup = new Setup(new FakeProvider(), maxOutputTokensPerLot: 1_500, maxParallelLots: 1);
        var runs = setup.JobRuns();
        var started = new List<(string Slot, int Lot)>();

        await setup.RunAsync(runs, _ => Task.CompletedTask,
            run => { lock (started) started.Add((run.Slot!, run.LotNumber!.Value)); return Task.CompletedTask; });

        Assert.True(setup.Context.Plan.Lots.Count > 1);
        foreach (var slot in new[] { "A", "B" })
        {
            var order = started.Where(s => s.Slot == slot).Select(s => s.Lot).ToList();
            Assert.Equal(Enumerable.Range(1, setup.Context.Plan.Lots.Count), order);
        }
    }

    [Fact]
    public async Task A_failure_in_one_run_does_not_stop_the_other()
    {
        using var setup = new Setup(new FailingModelProvider(ModelA));
        var outcomes = new Dictionary<string, RunOutcome>();

        await setup.RunAsync(setup.JobRuns(), o => { lock (outcomes) outcomes[o.Run.Slot!] = o; return Task.CompletedTask; });

        Assert.IsType<IOException>(outcomes["A"].Error);
        Assert.Null(outcomes["A"].Attempts);
        Assert.Equal(RunStatus.Done, Assert.Single(outcomes["B"].Attempts!).Result.Status);
    }

    [Fact]
    public async Task Each_run_has_its_own_folder_and_event_stream()
    {
        using var setup = new Setup(new FakeProvider());
        var runs = setup.JobRuns();

        await setup.RunAsync(runs, _ => Task.CompletedTask);

        foreach (var run in runs)
        {
            var folder = setup.Storage.RunFolder(run.Id);
            Assert.True(File.Exists(Path.Combine(folder, CatalogTask.FragmentsFile)));
            Assert.True(File.Exists(Path.Combine(folder, CatalogTask.OutputFile)));

            setup.Events.Complete(run.Id);
            var modelEvent = setup.Events.Follow(run.Id, CancellationToken.None).ToBlockingEnumerable()
                .Single(e => e.Kind == RunEventKind.ModelInfo);
            Assert.Contains(run.Model, modelEvent.Text);
        }
    }

    [Fact]
    public void Models_a_and_b_default_to_sol_and_luna()
    {
        var options = new RunnerOptions();

        Assert.Equal(ModelA, options.ModelA);
        Assert.Equal(ModelB, options.ModelB);
    }
}
