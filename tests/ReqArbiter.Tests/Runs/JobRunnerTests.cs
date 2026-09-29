using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Runner;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Tests.Runs;

// The runs of a job (A and B) without the database: parallelism, isolation
// between the two runs and separate folders and event streams.
public class JobRunnerTests
{
    private const string ModelA = "SS - GPT-6-SOL";
    private const string ModelB = "SS - GPT-6-LUNA";

    // Everything JobRunner needs, with runs stored under a temp folder.
    private sealed class Setup : IDisposable
    {
        private readonly TempDirectory _temp = new();

        public Setup(IRunnerProvider provider)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = _temp.Path })
                .Build();
            Storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = _temp.Path });
            Runner = new JobRunner(new RunExecutor(provider, Events, Storage, new RunnerOptions()));

            // One project document, as the extracted text the worker would pass.
            var text = Path.Combine(_temp.Path, "doc.extracted.txt");
            File.WriteAllText(text, "# ReservaSalas");
            Inputs = [new RunInputFile("reservasalas.md", text)];
        }

        public RunEventStore Events { get; } = new();
        public FileStorage Storage { get; }
        public JobRunner Runner { get; }
        public IReadOnlyList<RunInputFile> Inputs { get; }

        public void Dispose() => _temp.Dispose();
    }

    private static List<Run> JobRuns()
    {
        var jobId = Guid.NewGuid();
        Run NewRun(string slot, string model) => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = 1,
            JobId = jobId,
            Slot = slot,
            Provider = "Fake",
            Model = model,
            State = RunState.Running,
        };
        return [NewRun("A", ModelA), NewRun("B", ModelB)];
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

    // Each run waits until the other one has also started, then continues.
    // If the runs went one after the other, the first would wait forever
    // (here: until the 10 s timeout makes the test fail).
    private sealed class BarrierProvider(int expectedRuns) : IRunnerProvider
    {
        private readonly FakeProvider _fake = new();
        private readonly TaskCompletionSource _allStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _started;
        public string Name => "Barrier";

        public async Task<RunResult> RunAsync(RunRequest request, Action<RunEvent> onEvent, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _started) == expectedRuns)
            {
                _allStarted.SetResult();
            }
            await _allStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return await _fake.RunAsync(request, onEvent, cancellationToken);
        }
    }

    [Fact]
    public async Task Both_runs_execute_at_the_same_time()
    {
        using var setup = new Setup(new BarrierProvider(expectedRuns: 2));
        var outcomes = new List<RunOutcome>();

        await setup.Runner.RunAsync(JobRuns(), setup.Inputs, o => { lock (outcomes) outcomes.Add(o); return Task.CompletedTask; }, CancellationToken.None);

        // A TimeoutException inside a run would show up as an Error here.
        Assert.Equal(2, outcomes.Count);
        Assert.All(outcomes, o => Assert.Null(o.Error));
        Assert.All(outcomes, o => Assert.Equal(RunStatus.Done, Assert.Single(o.Attempts!).Result.Status));
    }

    [Fact]
    public async Task A_failure_in_one_run_does_not_stop_the_other()
    {
        using var setup = new Setup(new FailingModelProvider(ModelA));
        var outcomes = new Dictionary<string, RunOutcome>();

        await setup.Runner.RunAsync(JobRuns(), setup.Inputs, o => { lock (outcomes) outcomes[o.Run.Slot!] = o; return Task.CompletedTask; }, CancellationToken.None);

        Assert.IsType<IOException>(outcomes["A"].Error);
        Assert.Null(outcomes["A"].Attempts);
        Assert.Equal(RunStatus.Done, Assert.Single(outcomes["B"].Attempts!).Result.Status);
    }

    [Fact]
    public async Task Each_run_has_its_own_folder_and_event_stream()
    {
        using var setup = new Setup(new FakeProvider());
        var runs = JobRuns();

        await setup.Runner.RunAsync(runs, setup.Inputs, _ => Task.CompletedTask, CancellationToken.None);

        foreach (var run in runs)
        {
            var folder = setup.Storage.RunFolder(run.Id);
            Assert.True(File.Exists(Path.Combine(folder, "input", "reservasalas.txt")));
            Assert.True(File.Exists(Path.Combine(folder, SummaryTask.OutputFile)));

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
