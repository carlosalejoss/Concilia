using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using ReqArbiter.Api.Files;
using ReqArbiter.Api.Runs;
using ReqArbiter.Core.Catalog;
using ReqArbiter.Core.Projects;
using ReqArbiter.Core.Runs;

namespace ReqArbiter.Tests.Runs;

// The catalog of one slot in a job: its Done lots merged and validated, the
// failed and unfinished ones listed apart.
public class JobCatalogTests
{
    private static readonly Project Project = new() { Id = 1, Name = "ReservaSalas", SystemName = "el sistema ReservaSalas", Language = "es" };

    private sealed class Setup : IDisposable
    {
        private readonly TempDirectory _temp = new();

        public Setup()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = _temp.Path })
                .Build();
            Storage = new FileStorage(configuration, new HostingEnvironment { ContentRootPath = _temp.Path });
            var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "prompts", "ejemplo", "fuentes", "DOC-01.txt"));
            // A small lot size splits the example into several lots.
            Plan = CatalogTask.Plan([new CatalogDocument("DOC-01", "DOC-01.txt", text)], new CatalogOptions { MaxOutputTokensPerLot = 1_500 });
        }

        public FileStorage Storage { get; }
        public CatalogPlan Plan { get; }
        public Guid JobId { get; } = Guid.NewGuid();

        // A run of slot A for the lot, with its input and, if Done, the catalog
        // FakeProvider would have written and its review (accepted catalog).
        public Run Lot(int number, RunState state, int attempt = 1)
        {
            var lot = Plan.Lots.Single(l => l.Number == number);
            var run = new Run
            {
                Id = Guid.NewGuid(), ProjectId = 1, JobId = JobId, Slot = "A", LotNumber = number, LotTotal = lot.Total,
                Attempt = attempt, Provider = "Fake", Model = "SS - GPT-6-SOL", State = state,
            };
            var folder = Storage.RunFolder(run.Id);
            CatalogTask.BuildRequest(run, Project, Plan, folder, new CatalogOptions(), new RunnerOptions());
            if (state == RunState.Done)
            {
                Directory.CreateDirectory(Path.Combine(folder, "output"));
                File.WriteAllText(Path.Combine(folder, CatalogTask.OutputFile), FakeCatalog.For(folder, mutated: false));
                CatalogReport.ValidateRunFolder(folder, Project.SystemName);
            }
            return run;
        }

        public void Dispose() => _temp.Dispose();
    }

    [Fact]
    public void All_lots_done_gives_one_valid_catalog_saved_in_the_job_folder()
    {
        using var setup = new Setup();
        var runs = setup.Plan.Lots.Select(l => setup.Lot(l.Number, RunState.Done)).ToList();

        var catalog = JobCatalog.Build("A", runs, Project.SystemName, setup.Plan, setup.Storage, setup.JobId);

        Assert.True(setup.Plan.Lots.Count > 2);
        Assert.Equal(Enumerable.Range(1, setup.Plan.Lots.Count), catalog.IncludedLots);
        Assert.True(catalog.Report!.Valid, string.Join("\n", catalog.Report.Errors));
        Assert.Equal($"{setup.Plan.Fragments.Count}/{setup.Plan.Fragments.Count}", catalog.Report.Metrics!.FragmentCoverage);
        Assert.True(File.Exists(Path.Combine(setup.Storage.JobFolder(setup.JobId), JobCatalog.CatalogFile("A"))));
    }

    [Fact]
    public void Failed_and_unfinished_lots_are_left_out_and_listed()
    {
        using var setup = new Setup();
        var runs = new List<Run>
        {
            setup.Lot(1, RunState.Failed),
            setup.Lot(1, RunState.Done, attempt: 2), // the repair that worked counts
            setup.Lot(2, RunState.Failed),
            setup.Lot(3, RunState.Running),
        };

        var catalog = JobCatalog.Build("A", runs, Project.SystemName, setup.Plan, setup.Storage, setup.JobId);

        Assert.Equal([1], catalog.IncludedLots);
        Assert.Equal(2, Assert.Single(catalog.FailedLots));
        Assert.Contains(3, catalog.PendingLots);
        Assert.True(catalog.Report!.Valid, string.Join("\n", catalog.Report.Errors));
        // Not saved while a lot is still running.
        Assert.False(File.Exists(Path.Combine(setup.Storage.JobFolder(setup.JobId), JobCatalog.CatalogFile("A"))));
    }

    [Fact]
    public void A_repair_that_went_wrong_keeps_the_catalog_of_the_attempt_that_asked_for_it()
    {
        using var setup = new Setup();
        var runs = new List<Run>
        {
            setup.Lot(1, RunState.Done),                // valid, but asked for a repair (C20, for example)
            setup.Lot(1, RunState.Failed, attempt: 2),  // the repair broke the catalog
        };

        var catalog = JobCatalog.Build("A", runs, Project.SystemName, setup.Plan, setup.Storage, setup.JobId);

        Assert.Equal([1], catalog.IncludedLots);
        Assert.DoesNotContain(1, catalog.FailedLots);
    }

    // The JSON the web receives: ASP.NET Core serializes with the "Web"
    // defaults (camelCase); the report keeps its own names and the catalog
    // keeps the snake_case of the schema, with the rendered "text".
    [Fact]
    public void The_json_has_the_names_the_web_expects()
    {
        using var setup = new Setup();
        var runs = setup.Plan.Lots.Select(l => setup.Lot(l.Number, RunState.Done)).ToList();
        var catalog = JobCatalog.Build("A", runs, Project.SystemName, setup.Plan, setup.Storage, setup.JobId);

        var json = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(catalog, System.Text.Json.JsonSerializerOptions.Web)).RootElement;

        Assert.Equal(setup.Plan.Lots.Count, json.GetProperty("includedLots").GetArrayLength());
        Assert.True(json.GetProperty("report").GetProperty("valid").GetBoolean());
        Assert.Equal(0, json.GetProperty("report").GetProperty("metrics").GetProperty("requisitos").GetInt32());
        Assert.True(json.GetProperty("catalog").GetProperty("discarded_statements").GetArrayLength() > 0);
    }

    [Fact]
    public void A_slot_without_done_lots_has_no_catalog()
    {
        using var setup = new Setup();

        var catalog = JobCatalog.Build("A", [setup.Lot(1, RunState.Running)], Project.SystemName, setup.Plan, setup.Storage, setup.JobId);

        Assert.Empty(catalog.IncludedLots);
        Assert.Null(catalog.Report);
        Assert.Null(catalog.Catalog);
    }
}
